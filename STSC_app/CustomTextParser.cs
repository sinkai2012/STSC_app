using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

using Panel = System.Windows.Controls.Panel;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;

using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;

namespace STSC_app
{
    public static class CustomTextParser
    {
        public static void ParseAndRender(Panel container, string rawText, News? page = null)
        {
            container.Children.Clear();
            if (string.IsNullOrEmpty(rawText)) return;

            string cleanText = rawText.Replace("\r", "").Replace(@"\n", "\n");
            string[] lines = cleanText.Split('\n');

            bool inCodeBlock = false;
            StackPanel? currentCodeBlockPanel = null;

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                // 1. 同一行内で独立したコードブロック ```囲い```
                if (trimmedLine.StartsWith("```") && trimmedLine.EndsWith("```") && trimmedLine.Length > 6)
                {
                    string content = trimmedLine.Substring(3, trimmedLine.Length - 6);
                    AddCodeBlockFrame(container, content);
                    continue;
                }

                // 2. 複数行コードブロック処理中
                if (inCodeBlock && currentCodeBlockPanel != null)
                {
                    int backtickIndex = line.IndexOf("```");
                    if (backtickIndex != -1)
                    {
                        string contentBefore = line.Substring(0, backtickIndex);
                        if (!string.IsNullOrEmpty(contentBefore))
                        {
                            AddCodeBlockLine(currentCodeBlockPanel, contentBefore);
                        }

                        inCodeBlock = false;
                        currentCodeBlockPanel = null;
                        continue;
                    }

                    AddCodeBlockLine(currentCodeBlockPanel, line);
                    continue;
                }

                // 3. 複数行コードブロックの開始
                if (!inCodeBlock && trimmedLine.StartsWith("```"))
                {
                    inCodeBlock = true;
                    currentCodeBlockPanel = CreateCodeBlockPanel(container);
                    string firstLineContent = trimmedLine.Substring(3).Trim();
                    if (!string.IsNullOrEmpty(firstLineContent))
                    {
                        AddCodeBlockLine(currentCodeBlockPanel, firstLineContent);
                    }
                    continue;
                }

                // 4. 通常行の描画
                AddLineToPanel(container, line, page);
            }
        }

        private static void AddCodeBlockFrame(Panel targetPanel, string content)
        {
            Border border = CreateCodeBlockBorder();
            StackPanel innerPanel = new StackPanel();
            AddCodeBlockLine(innerPanel, content);
            border.Child = innerPanel;
            targetPanel.Children.Add(border);
        }

        private static StackPanel CreateCodeBlockPanel(Panel targetPanel)
        {
            Border border = CreateCodeBlockBorder();
            StackPanel innerPanel = new StackPanel();
            border.Child = innerPanel;
            targetPanel.Children.Add(border);
            return innerPanel;
        }

        private static Border CreateCodeBlockBorder()
        {
            return new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D2D")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E3E3E")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 4, 0, 8)
            };
        }

        private static void AddCodeBlockLine(StackPanel panel, string text)
        {
            TextBlock tb = new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 12.5,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCDDDE")),
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(tb);
        }

        private static void AddLineToPanel(Panel targetPanel, string line, News? page)
        {
            Brush textBrush = Application.Current.TryFindResource("PrimaryTextBrush") as Brush ?? Brushes.Black;

            TextBlock textBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 2),
                FontSize = 13,
                Foreground = textBrush
            };

            string trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("# "))
            {
                textBlock.FontSize = 22;
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.Margin = new Thickness(0, 10, 0, 5);
                line = trimmedLine.Substring(2);
            }
            else if (trimmedLine.StartsWith("## "))
            {
                textBlock.FontSize = 18;
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.Margin = new Thickness(0, 8, 0, 4);
                line = trimmedLine.Substring(3);
            }
            else if (trimmedLine.StartsWith("### "))
            {
                textBlock.FontSize = 15;
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.Margin = new Thickness(0, 6, 0, 2);
                line = trimmedLine.Substring(4);
            }

            ParseInlines(textBlock, line, page);
            targetPanel.Children.Add(textBlock);
        }

        private static void ParseInlines(TextBlock textBlock, string text, News? page)
        {
            string pattern = @"(\[(?<label>[^\]]+)\]\((?<url>https?://[^\s\)]+)\))|(https?://[^\s]+)|(\*\*(?<bold>.*?)\*\*)|(__(?<under>.*?)__)|(~~(?<strike>.*?)~~)|(\*(?<italic>.*?)\*)";

            int lastIndex = 0;
            foreach (Match match in Regex.Matches(text, pattern))
            {
                if (match.Index > lastIndex)
                {
                    textBlock.Inlines.Add(new Run(text.Substring(lastIndex, match.Index - lastIndex)));
                }

                if (match.Groups["url"].Success)
                {
                    string label = match.Groups["label"].Value;
                    string url = match.Groups["url"].Value;
                    textBlock.Inlines.Add(CreateHyperlink(url, label, page));
                }
                else if (match.Value.StartsWith("http://") || match.Value.StartsWith("https://"))
                {
                    textBlock.Inlines.Add(CreateHyperlink(match.Value, match.Value, page));
                }
                else if (match.Groups["bold"].Success)
                {
                    textBlock.Inlines.Add(new Run(match.Groups["bold"].Value) { FontWeight = FontWeights.Bold });
                }
                else if (match.Groups["under"].Success)
                {
                    textBlock.Inlines.Add(new Run(match.Groups["under"].Value) { TextDecorations = TextDecorations.Underline });
                }
                else if (match.Groups["strike"].Success)
                {
                    textBlock.Inlines.Add(new Run(match.Groups["strike"].Value) { TextDecorations = TextDecorations.Strikethrough });
                }
                else if (match.Groups["italic"].Success)
                {
                    textBlock.Inlines.Add(new Run(match.Groups["italic"].Value) { FontStyle = FontStyles.Italic });
                }

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < text.Length)
            {
                textBlock.Inlines.Add(new Run(text.Substring(lastIndex)));
            }
        }

        private static Hyperlink CreateHyperlink(string url, string displayText, News? parentPage)
        {
            Hyperlink link = new Hyperlink(new Run(displayText))
            {
                NavigateUri = new Uri(url, UriKind.RelativeOrAbsolute),
                Foreground = Brushes.DeepSkyBlue
            };

            link.RequestNavigate += (sender, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = e.Uri.AbsoluteUri,
                        UseShellExecute = true
                    });
                }
                catch { }
                e.Handled = true;
            };

            link.MouseEnter += (sender, e) =>
            {
                if (parentPage != null &&
                    parentPage.FindName("UrlStatusBar") is Border statusBar &&
                    parentPage.FindName("UrlStatusBarText") is TextBlock statusText)
                {
                    statusText.Text = url;
                    statusBar.Visibility = Visibility.Visible;
                }
            };

            link.MouseLeave += (sender, e) =>
            {
                if (parentPage != null &&
                    parentPage.FindName("UrlStatusBar") is Border statusBar)
                {
                    statusBar.Visibility = Visibility.Collapsed;
                }
            };

            ContextMenu contextMenu = new ContextMenu();

            MenuItem copyUrlItem = new MenuItem { Header = "URLをコピー" };
            copyUrlItem.Click += (sender, e) =>
            {
                Clipboard.SetText(url);
            };

            MenuItem copyFullTextItem = new MenuItem { Header = "全文をコピー" };
            copyFullTextItem.Click += (sender, e) =>
            {
                parentPage?.CopyFullText_Click(sender, e);
            };

            contextMenu.Items.Add(copyUrlItem);
            contextMenu.Items.Add(copyFullTextItem);

            link.ContextMenu = contextMenu;

            return link;
        }
    }
}
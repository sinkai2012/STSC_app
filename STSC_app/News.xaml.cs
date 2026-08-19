using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace STSC_app
{
    // ★ タグ表示用モデル（複数選択フラグを追加）
    public class TagModel
    {
        public string Name { get; set; } = "";
        public bool IsSelected { get; set; } = false;
    }

    public partial class News : System.Windows.Controls.Page
    {
        private DateTime _lastFetchTime = DateTime.MinValue;
        private readonly TimeSpan _coolTime = TimeSpan.FromSeconds(10);

        private readonly DispatcherTimer _autoUpdateTimer = new DispatcherTimer();
        private readonly TimeSpan _autoUpdateInterval = TimeSpan.FromMinutes(2);

        private const string ListJsonUrl = "https://gist.githubusercontent.com/sinkai2012/dc49cbaf8eead285228ed389df4e5275/raw/stsc-news.json";

        private List<NewsItem> _allNewsList = new List<NewsItem>();
        private List<TagModel> _tagList = new List<TagModel>();

        public News()
        {
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                RefreshThemeUI();
                StartAutoUpdateTimer();
            };

            ThemeManager.ThemeChanged += OnThemeChanged;
            this.Unloaded += (s, e) =>
            {
                ThemeManager.ThemeChanged -= OnThemeChanged;
                StopAutoUpdateTimer();
            };

            _ = LoadNewsListAsync();
        }

        private void StartAutoUpdateTimer()
        {
            _autoUpdateTimer.Interval = _autoUpdateInterval;
            _autoUpdateTimer.Tick -= AutoUpdateTimer_Tick;
            _autoUpdateTimer.Tick += AutoUpdateTimer_Tick;
            _autoUpdateTimer.Start();
        }

        private void StopAutoUpdateTimer()
        {
            _autoUpdateTimer.Stop();
            _autoUpdateTimer.Tick -= AutoUpdateTimer_Tick;
        }

        private async void AutoUpdateTimer_Tick(object? sender, EventArgs e)
        {
            await LoadNewsListAsync(isAutoUpdate: true);
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                RefreshThemeUI();
            });
        }

        private void RefreshThemeUI()
        {
            var selectedItem = NewsSidebarListBox?.SelectedItem;

            ApplyFilter();

            if (NewsSidebarListBox != null)
            {
                if (selectedItem != null && NewsSidebarListBox.Items.Contains(selectedItem))
                {
                    NewsSidebarListBox.SelectedItem = selectedItem;
                }

                NewsSidebarListBox.Items.Refresh();
            }

            UpdateDetailView();
        }

        private void UpdateDetailTagColors(string kinds)
        {
            if (DetailTagItemsControl == null) return;

            for (int i = 0; i < DetailTagItemsControl.Items.Count; i++)
            {
                var container = DetailTagItemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container != null)
                {
                    var border = FindVisualChild<Border>(container);
                    if (border != null)
                    {
                        border.Background = GetTagBackgroundBrush(kinds);
                        if (border.Child is TextBlock tb)
                        {
                            tb.Foreground = GetTagForegroundBrush(kinds);
                        }
                    }
                }
            }
        }

        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild) return typedChild;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null) return childOfChild;
            }
            return null;
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var timeSinceLastFetch = DateTime.Now - _lastFetchTime;
            if (timeSinceLastFetch < _coolTime)
            {
                return;
            }

            if (sender is System.Windows.Controls.Button button)
            {
                button.IsEnabled = false;
                string originalText = "ニュース更新";

                try
                {
                    SetButtonText(button, "更新中...");
                    await LoadNewsListAsync();
                    _lastFetchTime = DateTime.Now;
                }
                finally
                {
                    _ = StartCoolDownTimerAsync(button, originalText);
                }
            }
        }

        private async Task StartCoolDownTimerAsync(System.Windows.Controls.Button button, string originalText)
        {
            while (true)
            {
                var timeSinceLastFetch = DateTime.Now - _lastFetchTime;
                var remaining = (_coolTime - timeSinceLastFetch).TotalSeconds;

                if (remaining <= 0)
                {
                    SetButtonText(button, originalText);
                    button.IsEnabled = true;
                    break;
                }

                SetButtonText(button, $"あと {Math.Ceiling(remaining)} 秒");
                await Task.Delay(1000);
            }
        }

        private void SetButtonText(System.Windows.Controls.Button button, string text)
        {
            if (button.Content is System.Windows.Controls.StackPanel sp)
            {
                var textBlock = sp.Children.OfType<System.Windows.Controls.TextBlock>().LastOrDefault();
                if (textBlock != null)
                {
                    textBlock.Text = text;
                    return;
                }
            }

            button.Content = text;
        }

        private async Task LoadNewsListAsync(bool isAutoUpdate = false)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "STSC_app");
                    client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
                    {
                        NoCache = true,
                        NoStore = true
                    };

                    string requestUrl = $"{ListJsonUrl}?t={DateTime.Now.Ticks}";
                    string jsonString = await client.GetStringAsync(requestUrl);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var newList = JsonSerializer.Deserialize<List<NewsItem>>(jsonString, options) ?? new List<NewsItem>();

                    string currentJson = JsonSerializer.Serialize(_allNewsList);
                    string fetchedJson = JsonSerializer.Serialize(newList);

                    if (currentJson != fetchedJson)
                    {
                        _allNewsList = newList;

                        var selectedId = (NewsSidebarListBox?.SelectedItem as NewsItem)?.id;

                        UpdateTagListBox();
                        ApplyFilter();

                        if (!string.IsNullOrEmpty(selectedId))
                        {
                            var reSelectItem = _allNewsList.FirstOrDefault(n => n.id == selectedId);
                            if (reSelectItem != null && NewsSidebarListBox != null)
                            {
                                NewsSidebarListBox.SelectedItem = reSelectItem;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!isAutoUpdate)
                {
                    System.Windows.MessageBox.Show($"ニュース一覧の取得に失敗しました:\n{ex.Message}");
                }
            }
        }

        private void UpdateTagListBox()
        {
            if (TagFilterComboBox == null || _allNewsList == null) return;

            var currentSelected = _tagList.Where(t => t.IsSelected).Select(t => t.Name).ToHashSet();

            _tagList = _allNewsList
                .Where(n => n.content?.tag != null)
                .SelectMany(n => n.content!.tag!)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .OrderBy(t => t)
                .Select(t => new TagModel
                {
                    Name = t,
                    IsSelected = currentSelected.Contains(t)
                })
                .ToList();

            TagFilterComboBox.ItemsSource = _tagList;
            UpdateTagComboBoxHeader();
        }

        private void TagCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (TagFilterComboBox != null)
            {
                TagFilterComboBox.SelectedIndex = -1;
            }

            UpdateTagComboBoxHeader();
            ApplyFilter();
        }

        private void UpdateTagComboBoxHeader()
        {
            if (TagFilterDisplayTextBlock == null) return;

            var selectedTagNames = _tagList.Where(t => t.IsSelected).Select(t => t.Name).ToList();

            if (selectedTagNames.Count == 0)
            {
                TagFilterDisplayTextBlock.Text = "すべて（未選択）";
            }
            else
            {
                TagFilterDisplayTextBlock.Text = string.Join(", ", selectedTagNames);
            }
        }

        private void ApplyFilter()
        {
            if (NewsSidebarListBox == null || _allNewsList == null) return;

            IEnumerable<NewsItem> filtered = _allNewsList;

            // 1. キーワード検索
            string keyword = SearchKeywordTextBox?.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(keyword))
            {
                filtered = filtered.Where(n =>
                    (n.content?.title != null && n.content.title.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (n.content?.tag != null && n.content.tag.Any(t => t.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                );
            }

            // 2. 種類 (kinds) フィルター
            if (KindsFilterComboBox?.SelectedItem is ComboBoxItem selectedKindItem)
            {
                string kindSelection = selectedKindItem.Content?.ToString() ?? "";

                if (kindSelection == "通常")
                {
                    filtered = filtered.Where(n => (n.content?.kinds ?? "normal").Equals("normal", StringComparison.OrdinalIgnoreCase));
                }
                else if (kindSelection == "警告")
                {
                    filtered = filtered.Where(n => (n.content?.kinds ?? "").Equals("warning", StringComparison.OrdinalIgnoreCase));
                }
                else if (kindSelection == "重要")
                {
                    filtered = filtered.Where(n => (n.content?.kinds ?? "").Equals("important", StringComparison.OrdinalIgnoreCase));
                }
            }

            // 2.5 タグ (Tag) 複数選択フィルター
            var selectedTagNames = _tagList.Where(t => t.IsSelected).Select(t => t.Name).ToList();
            if (selectedTagNames.Count > 0)
            {
                filtered = filtered.Where(n =>
                    n.content?.tag != null && selectedTagNames.Any(st => n.content.tag.Contains(st, StringComparer.OrdinalIgnoreCase))
                );
            }

            // 3. 投稿期間フィルター
            if (StartDatePicker?.SelectedDate.HasValue == true)
            {
                DateTime startDate = StartDatePicker.SelectedDate.Value.Date;
                filtered = filtered.Where(n =>
                    DateTime.TryParse(n.date, out DateTime itemDate) && itemDate.Date >= startDate
                );
            }

            if (EndDatePicker?.SelectedDate.HasValue == true)
            {
                DateTime endDate = EndDatePicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);
                filtered = filtered.Where(n =>
                    DateTime.TryParse(n.date, out DateTime itemDate) && itemDate <= endDate
                );
            }

            // ★ 4. 並び替え処理（公開日 ＋ IDによる二次比較）
            // 並び替え用 ComboBox (x:Name="SortOrderComboBox") から選択肢を取得（デフォルトは降順）
            bool isAscending = false;
            if (SortOrderComboBox?.SelectedItem is ComboBoxItem selectedSortItem)
            {
                string sortText = selectedSortItem.Content?.ToString() ?? "";
                if (sortText.Contains("昇順"))
                {
                    isAscending = true;
                }
            }

            List<NewsItem> sortedList;
            if (isAscending)
            {
                // 【昇順】：公開日が古い順 ➔ 公開日が同じ場合は ID が小さい順 (1が一番最初)
                sortedList = filtered
                    .OrderBy(n => DateTime.TryParse(n.date, out DateTime dt) ? dt : DateTime.MinValue)
                    .ThenBy(n => n, new NewsIdComparer(isAscending: true))
                    .ToList();
            }
            else
            {
                // 【降順】：公開日が最新順 ➔ 公開日が同じ場合は ID が大きい順 (最新)
                sortedList = filtered
                    .OrderByDescending(n => DateTime.TryParse(n.date, out DateTime dt) ? dt : DateTime.MinValue)
                    .ThenByDescending(n => n, new NewsIdComparer(isAscending: false))
                    .ToList();
            }

            NewsSidebarListBox.ItemsSource = null;
            NewsSidebarListBox.ItemsSource = sortedList;
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchKeywordTextBox != null) SearchKeywordTextBox.Text = "";
            if (KindsFilterComboBox != null) KindsFilterComboBox.SelectedIndex = 0;
            if (SortOrderComboBox != null) SortOrderComboBox.SelectedIndex = 0; // ソートをデフォルト(降順)に
            if (StartDatePicker != null) StartDatePicker.SelectedDate = null;
            if (EndDatePicker != null) EndDatePicker.SelectedDate = null;

            foreach (var tag in _tagList)
            {
                tag.IsSelected = false;
            }
            UpdateTagComboBoxHeader();
            if (TagFilterComboBox != null) TagFilterComboBox.Items.Refresh();

            ApplyFilter();
        }

        private void NewsSidebarListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDetailView();
        }

        private void DetailTagBorder_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Border tagBorder &&
                NewsSidebarListBox.SelectedItem is NewsItem selectedNews)
            {
                string kinds = selectedNews.content?.kinds ?? "normal";
                tagBorder.Background = GetTagBackgroundBrush(kinds);

                if (tagBorder.Child is System.Windows.Controls.TextBlock tagTextBlock)
                {
                    tagTextBlock.Foreground = GetTagForegroundBrush(kinds);
                }
            }
        }

        private void UpdateDetailView()
        {
            if (NewsSidebarListBox?.SelectedItem is NewsItem selectedNews)
            {
                string kinds = selectedNews.content?.kinds ?? "normal";

                DetailHeaderBorder.Background = GetHeaderBackgroundBrush(kinds);
                if (System.Windows.Application.Current.TryFindResource("PrimaryTextBrush") is System.Windows.Media.Brush textBrush)
                {
                    DetailTitleTextBlock.Foreground = textBrush;
                }

                string dateText = $"公開日: {selectedNews.date}";
                if (!string.IsNullOrWhiteSpace(selectedNews.updatedate))
                {
                    dateText += $" （更新日: {selectedNews.updatedate}）";
                }

                DetailDateTextBlock.Text = dateText;
                DetailTitleTextBlock.Text = selectedNews.content?.title;
                DetailTagItemsControl.ItemsSource = selectedNews.content?.tag;

                DetailContentPanel.Children.Clear();

                if (!string.IsNullOrEmpty(selectedNews.content?.text))
                {
                    CustomTextParser.ParseAndRender(DetailContentPanel, selectedNews.content.text, this);
                }

                if (!string.IsNullOrEmpty(selectedNews.url) && !string.IsNullOrEmpty(selectedNews.urlname))
                {
                    System.Windows.Controls.Button urlButton = CreateUrlButton(selectedNews.urlname, selectedNews.url);
                    DetailContentPanel.Children.Add(urlButton);
                }

                UpdateDetailTagColors(kinds);
            }
        }

        private void ListTagBorder_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Border tagBorder &&
                tagBorder.DataContext is string &&
                FindParentListBoxItem(tagBorder)?.DataContext is NewsItem newsItem)
            {
                string kinds = newsItem.content?.kinds ?? "normal";
                tagBorder.Background = GetTagBackgroundBrush(kinds);

                if (tagBorder.Child is System.Windows.Controls.TextBlock tagTextBlock)
                {
                    tagTextBlock.Foreground = GetTagForegroundBrush(kinds);
                }
            }
        }

        private bool IsDarkModeActive()
        {
            if (System.Windows.Application.Current.TryFindResource("PrimaryTextBrush") is System.Windows.Media.SolidColorBrush textBrush)
            {
                return textBrush.Color.R > 200 && textBrush.Color.G > 200 && textBrush.Color.B > 200;
            }
            return false;
        }

        private System.Windows.Media.Brush GetHeaderBackgroundBrush(string kinds)
        {
            bool isDark = IsDarkModeActive();

            switch (kinds.ToLower())
            {
                case "warning":
                    string warnHex = isDark ? "#5A4B14" : "#FFF3CD";
                    return new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(warnHex)
                    );

                case "important":
                    string impHex = isDark ? "#5A1D23" : "#F8D7DA";
                    return new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(impHex)
                    );

                default:
                    if (System.Windows.Application.Current.TryFindResource("CardBackgroundBrush") is System.Windows.Media.Brush brush)
                    {
                        return brush;
                    }
                    return new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(isDark ? "#2D2D2D" : "#F5F5F5")
                    );
            }
        }

        private System.Windows.Media.Brush GetTagBackgroundBrush(string kinds)
        {
            bool isDark = IsDarkModeActive();

            string colorHex = kinds.ToLower() switch
            {
                "warning" => isDark ? "#7A641A" : "#FFE082",
                "important" => isDark ? "#7A2730" : "#F5C2C7",
                _ => isDark ? "#3E3E3E" : "#E2E2E2"
            };

            return new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex)
            );
        }

        private System.Windows.Media.Brush GetTagForegroundBrush(string kinds)
        {
            bool isDark = IsDarkModeActive();

            string colorHex = kinds.ToLower() switch
            {
                "warning" => isDark ? "#FFECB3" : "#533F03",
                "important" => isDark ? "#F8D7DA" : "#842029",
                _ => isDark ? "#F5F5F5" : "#333333"
            };

            return new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex)
            );
        }

        private System.Windows.Controls.Button CreateUrlButton(string buttonText, string targetUrl)
        {
            System.Windows.Controls.Button btn = new System.Windows.Controls.Button
            {
                Content = buttonText,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Margin = new Thickness(0, 15, 0, 10),
                Padding = new Thickness(10, 5, 10, 5),
                FontSize = 12,
                FontWeight = System.Windows.FontWeights.Bold,
                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#007ACC")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            System.Windows.Controls.ControlTemplate btnTemplate = new System.Windows.Controls.ControlTemplate(typeof(System.Windows.Controls.Button));
            System.Windows.FrameworkElementFactory borderFactory = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.Border));
            borderFactory.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new CornerRadius(6));
            borderFactory.SetBinding(System.Windows.Controls.Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });

            System.Windows.FrameworkElementFactory contentPresenter = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.ContentPresenter));
            contentPresenter.SetValue(System.Windows.Controls.ContentPresenter.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            contentPresenter.SetValue(System.Windows.Controls.ContentPresenter.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
            contentPresenter.SetValue(System.Windows.Controls.ContentPresenter.MarginProperty, new System.Windows.TemplateBindingExtension(System.Windows.Controls.Button.PaddingProperty));

            borderFactory.AppendChild(contentPresenter);
            btnTemplate.VisualTree = borderFactory;

            System.Windows.Trigger mouseOverTrigger = new System.Windows.Trigger { Property = System.Windows.UIElement.IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new System.Windows.Setter(System.Windows.UIElement.OpacityProperty, 0.85));
            btnTemplate.Triggers.Add(mouseOverTrigger);

            btn.Template = btnTemplate;

            btn.Click += (sender, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = targetUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"リンクを開けませんでした:\n{ex.Message}");
                }
            };

            btn.MouseEnter += (sender, e) =>
            {
                if (UrlStatusBar != null && UrlStatusBarText != null)
                {
                    UrlStatusBarText.Text = targetUrl;
                    UrlStatusBar.Visibility = Visibility.Visible;
                }
            };

            btn.MouseLeave += (sender, e) =>
            {
                if (UrlStatusBar != null)
                {
                    UrlStatusBar.Visibility = Visibility.Collapsed;
                }
            };

            System.Windows.Controls.ContextMenu contextMenu = new System.Windows.Controls.ContextMenu();

            System.Windows.Controls.MenuItem copyUrlItem = new System.Windows.Controls.MenuItem { Header = "URLをコピー" };
            copyUrlItem.Click += (sender, e) =>
            {
                System.Windows.Clipboard.SetText(targetUrl);
            };

            System.Windows.Controls.MenuItem copyFullTextItem = new System.Windows.Controls.MenuItem { Header = "全文をコピー" };
            copyFullTextItem.Click += (sender, e) =>
            {
                CopyFullText_Click(sender, e);
            };

            contextMenu.Items.Add(copyUrlItem);
            contextMenu.Items.Add(copyFullTextItem);

            btn.ContextMenu = contextMenu;

            return btn;
        }

        private void CopyNewsId_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.DataContext is NewsItem newsItem)
            {
                if (!string.IsNullOrEmpty(newsItem.id))
                {
                    System.Windows.Clipboard.SetText(newsItem.id);
                }
            }
        }

        public void CopyFullText_Click(object sender, RoutedEventArgs e)
        {
            if (NewsSidebarListBox.SelectedItem is NewsItem selectedNews)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(selectedNews.content?.title);
                sb.AppendLine($"公開日: {selectedNews.date}");
                sb.AppendLine("---");
                sb.AppendLine(selectedNews.content?.text);

                System.Windows.Clipboard.SetText(sb.ToString());
            }
        }

        private System.Windows.Controls.ListBoxItem? FindParentListBoxItem(DependencyObject child)
        {
            DependencyObject parent = System.Windows.Media.VisualTreeHelper.GetParent(child);

            while (parent != null && !(parent is System.Windows.Controls.ListBoxItem))
            {
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }

            return parent as System.Windows.Controls.ListBoxItem;
        }
    }

    // ★ ID比較用カスタムコンパララー（数値・英数字を正しく比較）
    public class NewsIdComparer : IComparer<NewsItem>
    {
        private readonly bool _isAscending;

        public NewsIdComparer(bool isAscending)
        {
            _isAscending = isAscending;
        }

        public int Compare(NewsItem? x, NewsItem? y)
        {
            string idX = x?.id ?? "";
            string idY = y?.id ?? "";

            // 数値変換を試みる（例: "10" と "2" を比較した場合に "10" の方が大きく評価されるようにする）
            bool isNumX = long.TryParse(idX, out long numX);
            bool isNumY = long.TryParse(idY, out long numY);

            if (isNumX && isNumY)
            {
                return numX.CompareTo(numY);
            }

            // 英数字を含む場合は文字列として比較
            return string.Compare(idX, idY, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class NewsContent
    {
        public List<string>? tag { get; set; }
        public string? kinds { get; set; }
        public string? title { get; set; }
        public string? text { get; set; }
    }

    public class NewsItem
    {
        public string? id { get; set; }
        public string? date { get; set; }
        public string? updatedate { get; set; }
        public NewsContent? content { get; set; }
        public string? urlname { get; set; }
        public string? url { get; set; }
    }
}
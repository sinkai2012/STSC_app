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

namespace STSC_app
{
    // タグ表示用モデル
    public class TagModel
    {
        public string Name { get; set; } = "";
    }

    public partial class News : System.Windows.Controls.Page
    {
        // ★ クラス直下に移動（ビルドエラー解消）
        private DateTime _lastFetchTime = DateTime.MinValue;
        private readonly TimeSpan _coolTime = TimeSpan.FromSeconds(10);

        private const string ListJsonUrl = "https://gist.githubusercontent.com/sinkai2012/dc49cbaf8eead285228ed389df4e5275/raw/stsc-news.json";

        // 全ニュースデータを保持する元リスト
        private List<NewsItem> _allNewsList = new List<NewsItem>();

        public News()
        {
            InitializeComponent();
            _ = LoadNewsListAsync();
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. 前回の取得からクールタイムが経過しているかチェック
            var timeSinceLastFetch = DateTime.Now - _lastFetchTime;
            if (timeSinceLastFetch < _coolTime)
            {
                // クールタイム中の場合は何もしない（メッセージボックスも出さない）
                return;
            }

            if (sender is System.Windows.Controls.Button button)
            {
                // ボタンを無効化
                button.IsEnabled = false;
                string originalText = "ニュース更新";

                try
                {
                    button.Content = "更新中...";

                    // 処理本体を実行
                    await LoadNewsListAsync();

                    // 成功した場合のみ最終取得日時を更新
                    _lastFetchTime = DateTime.Now;
                }
                finally
                {
                    // クールタイム中のカウントダウン処理（バックグラウンドでカウント）
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
                    // クールタイム終了
                    button.Content = originalText;
                    button.IsEnabled = true;
                    break;
                }

                // 残り秒数をボタンに表示
                button.Content = $"あと {Math.Ceiling(remaining)} 秒";

                // 1秒待機
                await Task.Delay(1000);
            }
        }

        private async Task LoadNewsListAsync()
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

                    _allNewsList = JsonSerializer.Deserialize<List<NewsItem>>(jsonString, options) ?? new List<NewsItem>();

                    // JSON内のタグを自動収集して ListBox にセット
                    UpdateTagListBox();

                    // フィルター適用＆表示更新
                    ApplyFilter();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"ニュース一覧の取得に失敗しました:\n{ex.Message}");
            }
        }

        // JSONからタグだけを取り出して ListBox にバインド
        private void UpdateTagListBox()
        {
            if (TagFilterListBox == null || _allNewsList == null) return;

            var allTags = _allNewsList
                .Where(n => n.content?.tag != null)
                .SelectMany(n => n.content!.tag!)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .OrderBy(t => t)
                .Select(t => new TagModel { Name = t })
                .ToList();

            TagFilterListBox.ItemsSource = allTags;
        }

        // フィルター＆ソート処理
        private void ApplyFilter()
        {
            if (NewsSidebarListBox == null || _allNewsList == null) return;

            IEnumerable<NewsItem> filtered = _allNewsList;

            // 1. キーワード検索（タイトル または タグ に含まれるか）
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
                if (kindSelection.StartsWith("normal"))
                {
                    filtered = filtered.Where(n => (n.content?.kinds ?? "normal").Equals("normal", StringComparison.OrdinalIgnoreCase));
                }
                else if (kindSelection.StartsWith("warning"))
                {
                    filtered = filtered.Where(n => (n.content?.kinds ?? "").Equals("warning", StringComparison.OrdinalIgnoreCase));
                }
                else if (kindSelection.StartsWith("important"))
                {
                    filtered = filtered.Where(n => (n.content?.kinds ?? "").Equals("important", StringComparison.OrdinalIgnoreCase));
                }
            }

            // 2.5 タグ (Tag) 複数選択フィルター (ListBox)
            if (TagFilterListBox != null && TagFilterListBox.SelectedItems.Count > 0)
            {
                // 選択されているタグの名称リストを取得
                var selectedTagNames = TagFilterListBox.SelectedItems
                    .Cast<TagModel>()
                    .Select(t => t.Name)
                    .ToList();

                // 選択タグのいずれか1つ以上（OR検索）を含むニュースを抽出
                filtered = filtered.Where(n =>
                    n.content?.tag != null && selectedTagNames.Any(st => n.content.tag.Contains(st, StringComparer.OrdinalIgnoreCase))
                );
            }

            // 3. 投稿期間 (DatePicker) フィルター
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

            // 4. 公開日順（最新順 / 降順）に並び替え
            List<NewsItem> sortedList = filtered
                .OrderByDescending(n => DateTime.TryParse(n.date, out DateTime dt) ? dt : DateTime.MinValue)
                .ToList();

            // UIを更新
            NewsSidebarListBox.ItemsSource = null;
            NewsSidebarListBox.ItemsSource = sortedList;
        }

        // 検索条件変更時の共通イベント
        private void Filter_Changed(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        // 検索クリアボタン
        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchKeywordTextBox != null) SearchKeywordTextBox.Text = "";
            if (KindsFilterComboBox != null) KindsFilterComboBox.SelectedIndex = 0;
            if (StartDatePicker != null) StartDatePicker.SelectedDate = null;
            if (EndDatePicker != null) EndDatePicker.SelectedDate = null;

            // タグの全選択解除（すべて表示状態にする）
            if (TagFilterListBox != null)
            {
                TagFilterListBox.UnselectAll();
            }

            ApplyFilter();
        }

        // ニュース選択時の処理
        private void NewsSidebarListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NewsSidebarListBox.SelectedItem is NewsItem selectedNews)
            {
                string kinds = selectedNews.content?.kinds ?? "normal";

                DetailHeaderBorder.Background = GetHeaderBackgroundBrush(kinds);
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
            }
        }

        // タグ読み込み時の色適用
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

        private System.Windows.Media.Brush GetHeaderBackgroundBrush(string kinds)
        {
            string colorHex = kinds.ToLower() switch
            {
                "warning" => "#FFF3CD",
                "important" => "#F8D7DA",
                _ => "#F5F5F5"
            };

            return new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex)
            );
        }

        private System.Windows.Media.Brush GetTagBackgroundBrush(string kinds)
        {
            string colorHex = kinds.ToLower() switch
            {
                "warning" => "#FFE082",
                "important" => "#F5C2C7",
                _ => "#E2E2E2"
            };

            return new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex)
            );
        }

        private System.Windows.Media.Brush GetTagForegroundBrush(string kinds)
        {
            string colorHex = kinds.ToLower() switch
            {
                "warning" => "#533F03",
                "important" => "#842029",
                _ => "#333333"
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
                Padding = new Thickness(12, 6, 12, 6),
                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#007ACC")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

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
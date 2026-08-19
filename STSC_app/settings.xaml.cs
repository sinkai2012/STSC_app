using System;
using System.Diagnostics;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace STSC_app
{
    public partial class Settings : Page
    {
        private SoundPlayer? player;

        public Settings()
        {
            InitializeComponent();

            Loaded += Settings_Loaded;

            VersionTextBlock.Text = $"v{App.AppVer}";
            DateTextBlock.Text = $"{App.AppDate}";

            // リソースから wav ファイルを読み込む
            try
            {
                var uri = new Uri("pack://application:,,,/Recycle.wav");
                var streamInfo = System.Windows.Application.GetResourceStream(uri);
                if (streamInfo != null)
                {
                    player = new SoundPlayer(streamInfo.Stream);
                    player.Load(); // 事前ロード
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"音声読み込みエラー: {ex.Message}");
            }
        }

        // 画面読み込み時の処理（テーマ反映 ＋ GitHubバージョン一覧 ＋ 自動更新設定の読み込み）
        private async void Settings_Loaded(object sender, RoutedEventArgs e)
        {
            if (ThemeComboBox != null)
            {
                ThemeComboBox.SelectionChanged -= ThemeComboBox_SelectionChanged;

                string savedTheme = AppSettings.Default.Theme;

                foreach (ComboBoxItem item in ThemeComboBox.Items)
                {
                    if (item.Tag?.ToString() == savedTheme)
                    {
                        ThemeComboBox.SelectedItem = item;
                        break;
                    }
                }

                if (ThemeComboBox.SelectedIndex == -1)
                {
                    ThemeComboBox.SelectedIndex = 2; // Tag="System"
                }

                ThemeComboBox.SelectionChanged += ThemeComboBox_SelectionChanged;
            }

            // ★ 保存されている自動更新チェック状態を反映
            if (DisableAutoUpdateCheckBox != null)
            {
                DisableAutoUpdateCheckBox.IsChecked = AppSettings.Default.DisableAutoUpdate;
            }

            // GitHub からバージョン一覧を取得して ComboBox にセット
            await LoadVersionListAsync();
        }

        // ★ 自動確認無効化チェックボックスの切り替えイベント
        private void DisableAutoUpdateCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (DisableAutoUpdateCheckBox != null)
            {
                AppSettings.Default.DisableAutoUpdate = DisableAutoUpdateCheckBox.IsChecked == true;
                AppSettings.Default.Save();
            }
        }

        // GitHub のリリース一覧を取得して ComboBox に割り当てる
        private async System.Threading.Tasks.Task LoadVersionListAsync()
        {
            if (VersionSelectComboBox == null) return;

            var versions = await UpdateChecker.GetVersionListAsync();
            VersionSelectComboBox.ItemsSource = versions;

            if (versions.Count > 0)
            {
                VersionSelectComboBox.SelectedIndex = 0; // 先頭（最新リリース）をデフォルト選択
            }
        }

        // 特定のバージョンをインストールするボタンのイベント（CustomDialog 適用）
        private async void InstallSelectedVersionButton_Click(object sender, RoutedEventArgs e)
        {
            if (VersionSelectComboBox?.SelectedItem is string selectedVersion)
            {
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    bool isYes = await mainWindow.ShowCustomDialogAsync(
                        "バージョンの切り替え",
                        $"バージョン '{selectedVersion}' をダウンロードしてインストールしますか？\n（完了後にアプリは自動再起動されます）",
                        "インストール",
                        "キャンセル"
                    );

                    if (isYes)
                    {
                        await UpdateChecker.InstallSpecificVersionAsync(selectedVersion);
                    }
                }
            }
        }

        // プルダウン変更時のイベント
        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                string themeTag = selectedItem.Tag.ToString()!;

                AppSettings.Default.Theme = themeTag;
                AppSettings.Default.Save();

                ApplyAppTheme(themeTag);
            }
        }

        private void ApplyAppTheme(string theme)
        {
            switch (theme)
            {
                case "Light":
                    break;
                case "Dark":
                    break;
                case "System":
                    break;
            }
        }

        // ハイパーリンククリック（CustomDialog 適用）
        private async void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    await mainWindow.ShowCustomDialogAsync("エラー", $"リンクを開けませんでした:\n{ex.Message}", "OK", "");
                }
            }
        }

        // キャッシュ削除ボタン（CustomDialog で非同期・一括処理）
        private async void ClearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                bool isYes = await mainWindow.ShowCustomDialogAsync("確認", "一時キャッシュを削除しますか？", "削除する", "キャンセル");

                if (isYes)
                {
                    player?.Play();
                    await mainWindow.ShowCustomDialogAsync("完了", "キャッシュを削除しました。", "閉じる", "");
                }
            }
        }

        // 「アップデートを確認」ボタンのイベント
        private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            await UpdateChecker.CheckUpdateAsync(isManualCheck: true);
        }

        private void ApplyThemeButton_Click(object sender, RoutedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                string themeTag = selectedItem.Tag.ToString()!;

                AppSettings.Default.Theme = themeTag;
                AppSettings.Default.Save();

                ThemeManager.ApplyTheme(themeTag);
            }
        }
    }
}
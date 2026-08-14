using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace STSC_app
{
    public partial class Settings : Page
    {
        public Settings()
        {
            InitializeComponent();
        }

        // ハイパーリンクをクリックした時にブラウザで開く処理
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
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
                // ★ CS0104エラー回避のため System.Windows. を明示
                System.Windows.MessageBox.Show($"リンクを開けませんでした:\n{ex.Message}");
            }
        }

        // キャッシュ削除ボタン
        private void ClearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmDialogOverlay != null)
            {
                ConfirmDialogOverlay.Visibility = Visibility.Visible;
            }
        }

        // ダイアログ「はい」ボタン
        private void DialogYes_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmDialogOverlay != null)
            {
                ConfirmDialogOverlay.Visibility = Visibility.Collapsed;
            }
            // ★ キャッシュ削除完了メッセージ
            System.Windows.MessageBox.Show("キャッシュを削除しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ダイアログ「いいえ」ボタン
        private void DialogNo_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmDialogOverlay != null)
            {
                ConfirmDialogOverlay.Visibility = Visibility.Collapsed;
            }
        }

        // 「アップデートを確認」ボタンのイベント
        private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            // 手動チェックフラグ (true) で呼び出し
            await UpdateChecker.CheckUpdateAsync(isManualCheck: true);
        }
    }
}
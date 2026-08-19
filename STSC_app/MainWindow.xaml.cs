using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace STSC_app
{
    public partial class MainWindow : Window
    {
        private Home? _homePage;
        private News? _newsPage;
        private Contact? _contactPage;

        public MainWindow()
        {
            InitializeComponent();

            // 初期画面（ホーム）の表示
            _homePage = new Home();
            MainFrame.Navigate(_homePage);

            // ナビゲーション後に履歴を消去するイベントを登録
            MainFrame.Navigated += MainFrame_Navigated;

            // 画面読み込み完了時に規約チェックを実行
            this.Loaded += (s, e) => CheckFirstLaunchTerms();
        }

        // ★ アプリ全体から呼び出せる汎用ダイアログ表示メソッド
        public async Task<bool> ShowCustomDialogAsync(string title, string message, string yesText = "はい", string noText = "いいえ")
        {
            if (MainOverlayGrid == null) return false;

            var dialog = new CustomDialog();
            MainOverlayGrid.Children.Add(dialog);

            // ユーザーの操作完了を待機
            bool result = await dialog.ShowAsync(title, message, yesText, noText);

            // 画面から消去
            MainOverlayGrid.Children.Remove(dialog);

            return result;
        }

        // 初回起動時の利用規約チェック処理
        private void CheckFirstLaunchTerms()
        {
            if (!STSC_app.AppSettings.Default.IsTermsAgreed)
            {
                var termsDialog = new TermsDialog();

                if (MainOverlayGrid != null)
                {
                    MainOverlayGrid.Children.Add(termsDialog);

                    termsDialog.OnAgreed += (s, e) =>
                    {
                        MainOverlayGrid.Children.Remove(termsDialog);

                        // ★ 同意直後かつ自動確認が無効化されていない場合のみチェック
                        if (!STSC_app.AppSettings.Default.DisableAutoUpdate)
                        {
                            _ = UpdateChecker.CheckUpdateAsync(isManualCheck: false);
                        }
                    };
                }
            }
            else
            {
                // ★ 2回目以降の起動時：自動確認が無効化されていない場合のみチェック
                if (!STSC_app.AppSettings.Default.DisableAutoUpdate)
                {
                    _ = UpdateChecker.CheckUpdateAsync(isManualCheck: false);
                }
            }
        }

        private void MainFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            while (MainFrame.CanGoBack)
            {
                MainFrame.RemoveBackEntry();
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            _homePage ??= new Home();
            if (MainFrame.Content != _homePage)
            {
                MainFrame.Navigate(_homePage);
            }
        }

        private void NewsButton_Click(object sender, RoutedEventArgs e)
        {
            _newsPage ??= new News();
            if (MainFrame.Content != _newsPage)
            {
                MainFrame.Navigate(_newsPage);
            }
        }

        private void ContactButton_Click(object sender, RoutedEventArgs e)
        {
            _contactPage ??= new Contact();
            if (MainFrame.Content != _contactPage)
            {
                MainFrame.Navigate(_contactPage);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Settings());
        }

        private void CatalogButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Catalog());
        }
    }
}
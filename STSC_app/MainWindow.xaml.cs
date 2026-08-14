using System.Windows;

namespace STSC_app
{
    public partial class MainWindow : Window
    {
        // ★ ページのインスタンスを1つだけ作って保持（使い回す）
        private Home? _homePage;
        private News? _newsPage;
        private Contact? _contactPage;

        public MainWindow()
        {
            InitializeComponent();

            // 起動時に自動でバージョン確認（通知がある時だけメッセージが出ます）
            _ = UpdateChecker.CheckUpdateAsync(isManualCheck: false);

            InitializeComponent();

            // 初期画面（ホーム）の表示
            _homePage = new Home();
            MainFrame.Navigate(_homePage);

            // ナビゲーション後に履歴を消去するイベントを登録
            MainFrame.Navigated += MainFrame_Navigated;
        }

        // 画面遷移が終わったら過去の履歴を消去してメモリ解放を促す
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
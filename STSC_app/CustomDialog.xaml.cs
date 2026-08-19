using System.Threading.Tasks;
using System.Windows;

namespace STSC_app
{
    // ★ System.Windows.Controls.UserControl と明示的に指定
    public partial class CustomDialog : System.Windows.Controls.UserControl
    {
        private TaskCompletionSource<bool>? _tcs;

        public CustomDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ダイアログを表示し、ボタン選択を非同期で待ちます
        /// </summary>
        public Task<bool> ShowAsync(string title, string message, string yesText = "はい", string noText = "いいえ")
        {
            TitleText.Text = title;
            MessageText.Text = message;
            YesButton.Content = yesText;

            // 「いいえ」のテキストが空文字の場合は非表示（「OK」のみのダイアログ用）
            if (string.IsNullOrEmpty(noText))
            {
                NoButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                NoButton.Content = noText;
                NoButton.Visibility = Visibility.Visible;
            }

            _tcs = new TaskCompletionSource<bool>();
            return _tcs.Task;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            _tcs?.TrySetResult(true);
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            _tcs?.TrySetResult(false);
        }
    }
}
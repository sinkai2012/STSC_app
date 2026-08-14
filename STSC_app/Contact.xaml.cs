using System.Windows.Controls;

namespace STSC_app
{
    public partial class Contact : Page
    {
        public Contact()
        {
            InitializeComponent();
            InitializeWebViewAsync();
        }

        private async void InitializeWebViewAsync()
        {
            // WebView2の初期化（非同期）
            await ContactWebView.EnsureCoreWebView2Async(null);
        }
    }
}
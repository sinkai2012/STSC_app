using System;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace STSC_app
{
    public partial class Catalog : Page
    {
        // 許可するベースURL
        private const string AllowedUrl = "https://sites.google.com/view/shinkai-traffic-signal/productsforsale";

        public Catalog()
        {
            InitializeComponent();
        }

        // URL遷移が発生したときにブロック判定する
        private void CatalogWebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            string targetUrl = e.Uri;

            // 許可URLで始まっていないページ（前のページや外部サイト）への遷移をブロック
            if (!targetUrl.StartsWith(AllowedUrl, StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true; // ナビゲーションを停止
            }
        }
    }
}
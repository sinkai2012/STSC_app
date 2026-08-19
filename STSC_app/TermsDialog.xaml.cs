using System;
using System.Windows;
using System.Windows.Controls;

namespace STSC_app
{
    public partial class TermsDialog : System.Windows.Controls.UserControl
    {
        // 同意したときに呼び出すイベント
        public event EventHandler? OnAgreed;

        public TermsDialog()
        {
            InitializeComponent();
        }

        private void AgreeCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // チェックボックスが入っているときだけ「開始する」ボタンを有効化
            AgreeButton.IsEnabled = AgreeCheckBox.IsChecked == true;
        }

        private void AgreeButton_Click(object sender, RoutedEventArgs e)
        {
            // ★ AppSettings を直接参照して保存
            STSC_app.AppSettings.Default.IsTermsAgreed = true;
            STSC_app.AppSettings.Default.Save();

            // イベント発火してダイアログを閉じる
            OnAgreed?.Invoke(this, EventArgs.Empty);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            // 同意しない場合はアプリを終了
            System.Windows.Application.Current.Shutdown();
        }
    }
}
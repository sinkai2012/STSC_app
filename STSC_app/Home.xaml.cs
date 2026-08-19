using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace STSC_app
{
    /// <summary>
    /// Home.xaml の相互作用ロジック
    /// </summary>
    public partial class Home : Page
    {
        public Home()
        {
            InitializeComponent();

            VersionTextBlock.Text = $"v{App.AppVer}";

            DateTextBlock.Text = $"{App.AppDate}";

            ReleaseNotes.Content = $"v{App.AppVer} のリリースノート";
        }
    }
}

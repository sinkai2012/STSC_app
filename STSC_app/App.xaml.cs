using System.Configuration;
using System.Data;
using System.Windows;

namespace STSC_app
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 保存されたテーマを適用
            string savedTheme = AppSettings.Default.Theme;
            if (string.IsNullOrEmpty(savedTheme)) savedTheme = "System";

            ThemeManager.ApplyTheme(savedTheme);
        }

        public static string AppVer { get; set; } = "1.0.0-beta";
        public static string AppDate { get; set; } = "2026-09-01 00:00:00";
    }

}

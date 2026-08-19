using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace STSC_app
{
    public static class ThemeManager
    {
        public static event EventHandler? ThemeChanged;
        private static string currentThemeSetting = "System";

        static ThemeManager()
        {
            // Windowsのシステム設定変更イベントを監視
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        }

        public static void ApplyTheme(string themeTag)
        {
            currentThemeSetting = themeTag;
            UpdateTheme();
        }

        private static void UpdateTheme()
        {
            ThemeChanged?.Invoke(null, EventArgs.Empty);
            string themeToApply = currentThemeSetting;

            if (currentThemeSetting == "System")
            {
                themeToApply = IsWindowsInDarkMode() ? "Dark" : "Light";
            }

            string themeUri = themeToApply == "Dark"
                ? "pack://application:,,,/STSC_app;component/Themes/DarkTheme.xaml"
                : "pack://application:,,,/STSC_app;component/Themes/LightTheme.xaml";

            var newDict = new ResourceDictionary
            {
                Source = new Uri(themeUri, UriKind.Absolute)
            };

            var appDicts = System.Windows.Application.Current.Resources.MergedDictionaries;

            // 既存のテーマ辞書をすべて削除して置き換え
            var oldDicts = appDicts.Where(d => d.Source != null && d.Source.OriginalString.IndexOf("Themes/", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            foreach (var dict in oldDicts)
            {
                appDicts.Remove(dict);
            }

            appDicts.Add(newDict);
        }

        // Windowsの設定が変更された時に自動発火
        private static void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            // 設定カテゴリの変更かつ「デバイスに合わせる」選択時のみ再適用
            if (e.Category == UserPreferenceCategory.General && currentThemeSetting == "System")
            {
                // UIスレッドで安全にテーマを更新
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateTheme();
                });
            }
        }

        // Windowsがダークモードか判定
        public static bool IsWindowsInDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int intValue && intValue == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
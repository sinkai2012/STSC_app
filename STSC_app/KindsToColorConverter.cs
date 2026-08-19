using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

using ColorConverter = System.Windows.Media.ColorConverter;
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using Application = System.Windows.Application;

namespace STSC_app
{
    public class KindsToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? kind = value as string;

            // ★ 現在適用されているテキスト色からダークモードか動的に判定
            bool isDark = false;
            if (Application.Current.TryFindResource("PrimaryTextBrush") is SolidColorBrush textBrush)
            {
                // 文字色が明るい色（白系）ならダークモードと判断
                isDark = textBrush.Color.R > 200 && textBrush.Color.G > 200 && textBrush.Color.B > 200;
            }

            switch (kind?.ToLower())
            {
                case "important":
                    string impColor = isDark ? "#5A1D23" : "#FFE6E6";
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(impColor));

                case "warning":
                    string warnColor = isDark ? "#5A4B14" : "#FFF3CD";
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(warnColor));

                case "normal":
                default:
                    if (Application.Current.TryFindResource("CardBackgroundBrush") is Brush brush)
                    {
                        return brush;
                    }
                    return new SolidColorBrush(Color.FromRgb(240, 240, 240));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace STSC_app
{
    public class KindsToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? kind = value as string;

            switch (kind)
            {
                case "important":
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 230, 230));
                case "warning":
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 243, 205));
                case "normal":
                default:
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
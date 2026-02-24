using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ThunderApp.Converters
{
    public sealed class SeverityToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = (value as string ?? "").Trim();

            if (s.Equals("Extreme", StringComparison.OrdinalIgnoreCase))
                return new SolidColorBrush(Color.FromRgb(220, 38, 38));    // red
            if (s.Equals("Severe", StringComparison.OrdinalIgnoreCase))
                return new SolidColorBrush(Color.FromRgb(249, 115, 22));   // orange
            if (s.Equals("Moderate", StringComparison.OrdinalIgnoreCase))
                return new SolidColorBrush(Color.FromRgb(234, 179, 8));    // amber
            if (s.Equals("Minor", StringComparison.OrdinalIgnoreCase))
                return new SolidColorBrush(Color.FromRgb(59, 130, 246));   // blue

            return new SolidColorBrush(Color.FromRgb(100, 116, 139));      // gray (Unknown)
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
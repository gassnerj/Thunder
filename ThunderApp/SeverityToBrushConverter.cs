namespace ThunderApp;

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

public class SeverityToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string sev = value?.ToString()?.ToLower() ?? "";

        return sev switch
        {
            "extreme" => new SolidColorBrush(Color.FromRgb(185, 28, 28)), // red
            "severe" => new SolidColorBrush(Color.FromRgb(234, 88, 12)), // orange
            "moderate" => new SolidColorBrush(Color.FromRgb(217, 119, 6)), // amber
            "minor" => new SolidColorBrush(Color.FromRgb(37, 99, 235)), // blue
            _ => new SolidColorBrush(Color.FromRgb(107, 114, 128))
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
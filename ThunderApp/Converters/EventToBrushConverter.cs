using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ThunderApp.Services;

namespace ThunderApp.Converters;

/// <summary>
/// Maps NWS alert "Event" strings to the active hazard palette (official by default, optional custom overrides).
/// </summary>
public sealed class EventToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var ev = (value as string ?? string.Empty).Trim();
        return HazardColorPalette.GetBrush(ev);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

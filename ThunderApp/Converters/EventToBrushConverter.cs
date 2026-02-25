using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ThunderApp.Converters;

/// <summary>
/// Maps NWS alert "Event" strings to the official-ish WWA palette (where known).
/// Falls back to a neutral brush when unknown.
///
/// NOTE: We intentionally keep this list small and focused on common, high-impact hazards.
/// Add more mappings as you encounter them.
/// </summary>
public sealed class EventToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var ev = (value as string ?? string.Empty).Trim();
        if (ev.Length == 0) return Brushes.SlateGray;

        // Normalize
        var e = ev.ToLowerInvariant();

        // Core chase-critical
        if (e.Contains("tornado warning")) return BrushFromHex("#FF0000");
        if (e.Contains("severe thunderstorm warning")) return BrushFromHex("#FFA500");
        if (e.Contains("flash flood warning")) return BrushFromHex("#8B0000");
        if (e.Contains("extreme wind warning")) return BrushFromHex("#FF8C00");

        // Watches (common conventions)
        if (e.Contains("tornado watch")) return BrushFromHex("#FFCC00");
        if (e.Contains("severe thunderstorm watch")) return BrushFromHex("#FFCC00");

        // Flood products
        if (e.Contains("flood warning")) return BrushFromHex("#00A651");
        if (e.Contains("flood advisory")) return BrushFromHex("#00C17D");
        if (e.Contains("flood watch")) return BrushFromHex("#2E8B57");

        // Fire
        if (e.Contains("red flag warning")) return BrushFromHex("#FF1493");
        if (e.Contains("fire weather watch")) return BrushFromHex("#FF69B4");

        // Winter (basic)
        if (e.Contains("blizzard warning")) return BrushFromHex("#FF69B4");
        if (e.Contains("winter storm warning")) return BrushFromHex("#FF69B4");
        if (e.Contains("ice storm warning")) return BrushFromHex("#8A2BE2");

        return Brushes.SlateGray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static SolidColorBrush BrushFromHex(string hex)
    {
        var c = (Color)ColorConverter.ConvertFromString(hex);
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}

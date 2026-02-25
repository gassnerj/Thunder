using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace ThunderApp.Services;

public static class HazardColorPalette
{
    private static readonly object Gate = new();
    private static Dictionary<string, string> _official = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, string> _custom = new(StringComparer.OrdinalIgnoreCase);
    private static string _mode = "Official";

    public static event Action? PaletteChanged;

    public static void SetOfficial(Dictionary<string, string> official)
    {
        lock (Gate)
        {
            _official = new Dictionary<string, string>(official, StringComparer.OrdinalIgnoreCase);
        }
        PaletteChanged?.Invoke();
    }

    public static void SetCustom(string mode, Dictionary<string, string> customOverrides)
    {
        lock (Gate)
        {
            _mode = mode ?? "Official";
            _custom = new Dictionary<string, string>(customOverrides ?? new(), StringComparer.OrdinalIgnoreCase);
        }
        PaletteChanged?.Invoke();
    }

    public static string GetHex(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName)) return "#A0A0A0";

        lock (Gate)
        {
            if (string.Equals(_mode, "Custom", StringComparison.OrdinalIgnoreCase))
            {
                if (_custom.TryGetValue(eventName.Trim(), out var c) && !string.IsNullOrWhiteSpace(c))
                    return NormalizeHex(c);
            }

            if (_official.TryGetValue(eventName.Trim(), out var o) && !string.IsNullOrWhiteSpace(o))
                return NormalizeHex(o);

            // fallback
            return "#A0A0A0";
        }
    }

    
    public static Dictionary<string, string> GetEffectivePalette()
    {
        lock (Gate)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in _official) result[kv.Key] = NormalizeHex(kv.Value);
            if (string.Equals(_mode, "Custom", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var kv in _custom)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                    if (string.IsNullOrWhiteSpace(kv.Value)) continue;
                    result[kv.Key.Trim()] = NormalizeHex(kv.Value);
                }
            }
            return result;
        }
    }

    public static IReadOnlyList<(string EventName, string OfficialHex, string? CustomHex)> Snapshot()
    {
        lock (Gate)
        {
            var keys = _official.Keys.Concat(_custom.Keys).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(k => k);
            return keys.Select(k =>
            {
                _official.TryGetValue(k, out var o);
                _custom.TryGetValue(k, out var c);
                return (k, o ?? "#A0A0A0", string.IsNullOrWhiteSpace(c) ? null : NormalizeHex(c));
            }).ToList();
        }
    }

    
    public static bool TryGetOfficialHex(string eventName, out string hex)
    {
        hex = "";
        if (string.IsNullOrWhiteSpace(eventName)) return false;
        lock (Gate)
        {
            if (_official.TryGetValue(eventName.Trim(), out var o) && !string.IsNullOrWhiteSpace(o))
            {
                hex = NormalizeHex(o);
                return true;
            }
            return false;
        }
    }

    public static bool TryGetCustomHex(string eventName, out string? hex)
    {
        hex = null;
        if (string.IsNullOrWhiteSpace(eventName)) return false;
        lock (Gate)
        {
            if (_custom.TryGetValue(eventName.Trim(), out var c) && !string.IsNullOrWhiteSpace(c))
            {
                hex = NormalizeHex(c);
                return true;
            }
            return false;
        }
    }

    public static Brush GetBrush(string eventName)
    {
        var hex = GetHex(eventName);
        try
        {
            return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        }
        catch
        {
            return Brushes.SlateGray;
        }
    }

    private static string NormalizeHex(string hex)
    {
        hex = (hex ?? string.Empty).Trim();
        if (hex.Length == 0) return "#A0A0A0";

        if (!hex.StartsWith("#")) hex = "#" + hex;

        // #RRGGBB
        if (hex.Length == 7) return hex.ToUpperInvariant();

        // #AARRGGBB (supports alpha, e.g. transparent)
        if (hex.Length == 9) return hex.ToUpperInvariant();

        return "#A0A0A0";
    }
}

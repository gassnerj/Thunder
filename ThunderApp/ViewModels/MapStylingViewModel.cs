using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using ThunderApp.Models;
using ThunderApp.Services;

namespace ThunderApp.ViewModels;

public sealed partial class HazardColorItem : ObservableObject
{
    public string EventName { get; }
    public string OfficialHex { get; }

    [ObservableProperty] private string? customHex;

public Brush OfficialBrush => ToBrush(OfficialHex);

public Brush EffectiveCustomBrush => ToBrush(string.IsNullOrWhiteSpace(CustomHex) ? OfficialHex : CustomHex!);

public bool IsOfficialMissing => string.Equals(OfficialHex?.Trim(), "#A0A0A0", StringComparison.OrdinalIgnoreCase);

partial void OnCustomHexChanged(string? value)
{
    OnPropertyChanged(nameof(EffectiveCustomBrush));
}

private static Brush ToBrush(string hex)
{
    try
    {
        // Supports #RRGGBB and #AARRGGBB
        var c = (Color)ColorConverter.ConvertFromString(hex)!;
        return new SolidColorBrush(c);
    }
    catch
    {
        return Brushes.Gray;
    }
}

    public HazardColorItem(string eventName, string officialHex, string? customHex)
    {
        EventName = eventName;
        OfficialHex = officialHex;
        CustomHex = customHex;
    }
}

public sealed partial class MapStylingViewModel : ObservableObject
{
    private readonly AlertFilterSettings _settings;

    public ObservableCollection<HazardColorItem> Items { get; } = new();

    [ObservableProperty] private string paletteMode;
    [ObservableProperty] private bool showSeverityOutline;
    [ObservableProperty] private bool showSeverityGlow;
    [ObservableProperty] private bool showSeverityStripes;

    [ObservableProperty] private int alertsOpacityPercent;
    [ObservableProperty] private int spcOpacityPercent;

    public MapStylingViewModel(AlertFilterSettings settings)
    {
        _settings = settings;

        PaletteMode = settings.HazardPaletteMode;
        ShowSeverityOutline = settings.ShowSeverityOutline;
        ShowSeverityGlow = settings.ShowSeverityGlow;
        ShowSeverityStripes = settings.ShowSeverityStripes;
        AlertsOpacityPercent = settings.AlertsOpacityPercent;
        SpcOpacityPercent = settings.SpcOpacityPercent;

        RefreshItems();
    }

    public void RefreshItems()
    {
        Items.Clear();

        // Build a complete list:
        // - all official hazard colors (weather.gov list)
        // - all saved custom overrides
        // - all alert event types Thunder already knows about (AlertCatalog)
        var snap = HazardColorPalette.Snapshot();
        var byName = new Dictionary<string, (string Official, string? Custom)>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, officialHex, customHex) in snap)
            byName[name] = (officialHex, customHex);

        foreach (var def in AlertCatalog.All)
        {
            if (string.IsNullOrWhiteSpace(def.EventName)) continue;

            if (!byName.ContainsKey(def.EventName))
            {
                // Use official if we have it; otherwise fall back to gray (still editable by Custom).
                var off = HazardColorPalette.TryGetOfficialHex(def.EventName, out var oh) ? oh : "#A0A0A0";
                var cus = HazardColorPalette.TryGetCustomHex(def.EventName, out var ch) ? ch : null;
                byName[def.EventName] = (off, cus);
            }
        }

        foreach (var k in byName.Keys.OrderBy(k => k))
        {
            var v = byName[k];
            Items.Add(new HazardColorItem(k, v.Official, v.Custom));
        }
    }

    public void SaveToSettings()
    {
        _settings.HazardPaletteMode = PaletteMode;
        _settings.ShowSeverityOutline = ShowSeverityOutline;
        _settings.ShowSeverityGlow = ShowSeverityGlow;
        _settings.ShowSeverityStripes = ShowSeverityStripes;
        _settings.AlertsOpacityPercent = Math.Clamp(AlertsOpacityPercent, 0, 100);
        _settings.SpcOpacityPercent = Math.Clamp(SpcOpacityPercent, 0, 100);

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var it in Items)
        {
            var hex = (it.CustomHex ?? "").Trim();
            if (hex.Length == 0) continue;
            dict[it.EventName] = hex;
        }

        // Replace the dictionary so PropertyChanged fires reliably
        _settings.CustomHazardColors = dict;
    }

    public void ClearCustomColors()
    {
        foreach (var it in Items)
            it.CustomHex = null;
    }
}

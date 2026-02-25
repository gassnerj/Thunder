using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using ThunderApp.Models;
using ThunderApp.Services;

namespace ThunderApp.ViewModels;

public sealed partial class HazardColorItem : ObservableObject
{
    public string EventName { get; }
    public string OfficialHex { get; }

    [ObservableProperty] private string? customHex;

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

    public MapStylingViewModel(AlertFilterSettings settings)
    {
        _settings = settings;

        PaletteMode = settings.HazardPaletteMode;
        ShowSeverityOutline = settings.ShowSeverityOutline;
        ShowSeverityGlow = settings.ShowSeverityGlow;
        ShowSeverityStripes = settings.ShowSeverityStripes;

        RefreshItems();
    }

    public void RefreshItems()
    {
        Items.Clear();
        foreach (var (name, officialHex, customHex) in HazardColorPalette.Snapshot())
            Items.Add(new HazardColorItem(name, officialHex, customHex));
    }

    public void SaveToSettings()
    {
        _settings.HazardPaletteMode = PaletteMode;
        _settings.ShowSeverityOutline = ShowSeverityOutline;
        _settings.ShowSeverityGlow = ShowSeverityGlow;
        _settings.ShowSeverityStripes = ShowSeverityStripes;

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

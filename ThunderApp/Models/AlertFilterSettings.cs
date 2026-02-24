using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ThunderApp.Models;

public partial class AlertFilterSettings : ObservableObject
{
    // severity toggles (you already use these)
    [ObservableProperty] private bool showExtreme = true;
    [ObservableProperty] private bool showSevere = true;
    [ObservableProperty] private bool showModerate = true;
    [ObservableProperty] private bool showMinor = true;
    [ObservableProperty] private bool showUnknown = true;

    // near-me
    [ObservableProperty] private bool useNearMe = false;
    [ObservableProperty] private double manualLat = 0;
    [ObservableProperty] private double manualLon = 0;

    // WeatherFront-style category chips
    [ObservableProperty] private AlertCategory selectedCategory = AlertCategory.Severe;

    // If an event is in this set => HIDDEN
    // (we store "disabled" because it's easier to persist deltas)
    [ObservableProperty]
    private HashSet<string> disabledEvents = new(StringComparer.OrdinalIgnoreCase);

    public bool IsEventEnabled(string evt)
        => !DisabledEvents.Contains(evt);

    public void SetEventEnabled(string evt, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(evt)) return;

        evt = evt.Trim();

        if (enabled)
            DisabledEvents.Remove(evt);
        else
            DisabledEvents.Add(evt);

        // Notify that DisabledEvents effectively changed (for filters)
        OnPropertyChanged(nameof(DisabledEvents));
    }
}
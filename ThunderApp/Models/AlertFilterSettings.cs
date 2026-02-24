using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ThunderApp.Models;

public partial class AlertFilterSettings : ObservableObject
{
    // existing ones you already have (example pattern)
    [ObservableProperty] private bool showExtreme = true;
    [ObservableProperty] private bool showSevere = true;
    [ObservableProperty] private bool showModerate = true;
    [ObservableProperty] private bool showMinor = true;
    [ObservableProperty] private bool showUnknown = true;

    [ObservableProperty] private bool useNearMe = false;
    [ObservableProperty] private double manualLat = 0;
    [ObservableProperty] private double manualLon = 0;

    // new type toggles
    [ObservableProperty] private bool showTornado = true;
    [ObservableProperty] private bool showSevereThunderstorm = true;
    [ObservableProperty] private bool showFlashFlood = true;
    [ObservableProperty] private bool showWinter = true;
    [ObservableProperty] private bool showOther = true;

    // keep your existing collection(s)
    [ObservableProperty]
    private HashSet<string> hiddenEvents = new(StringComparer.OrdinalIgnoreCase);
    
    public event EventHandler? HiddenEventsChanged;

    public bool HideEvent(string evt)
    {
        if (string.IsNullOrWhiteSpace(evt)) return false;

        var added = HiddenEvents.Add(evt.Trim());
        if (added) HiddenEventsChanged?.Invoke(this, EventArgs.Empty);
        return added;
    }

    public bool UnhideEvent(string evt)
    {
        if (string.IsNullOrWhiteSpace(evt)) return false;

        var removed = HiddenEvents.Remove(evt.Trim());
        if (removed) HiddenEventsChanged?.Invoke(this, EventArgs.Empty);
        return removed;
    }

    public void ClearHiddenEvents()
    {
        if (HiddenEvents.Count == 0) return;
        HiddenEvents.Clear();
        HiddenEventsChanged?.Invoke(this, EventArgs.Empty);
    }
}
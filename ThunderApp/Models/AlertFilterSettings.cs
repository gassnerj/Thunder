using System;
using System.Collections.Generic;

namespace ThunderApp.Models;

// Models/AlertFilterSettings.cs
public sealed class AlertFilterSettings
{
    public bool UseNearMe { get; set; } = false;
    public double ManualLat { get; set; } = 33.9595562;   // whatever default you want
    public double ManualLon { get; set; } = -98.6837273;

    // quick toggles
    public bool ShowExtreme { get; set; } = true;
    public bool ShowSevere { get; set; } = true;
    public bool ShowModerate { get; set; } = true;
    public bool ShowMinor { get; set; } = true;
    public bool ShowUnknown { get; set; } = true;

    // optional: per-event toggles (you’ll likely expand this)
    public HashSet<string> HiddenEvents { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
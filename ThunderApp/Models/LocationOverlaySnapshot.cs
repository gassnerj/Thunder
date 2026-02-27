using System;

namespace ThunderApp.Models;

public sealed class LocationOverlaySnapshot
{
    public DateTime GeneratedAtUtc { get; set; }
    public string LocLine { get; set; } = "";
    public string LocDetail { get; set; } = "";

    public string Road { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";

    public double DistMi { get; set; }
    public string Dir { get; set; } = "";

    public double Lat { get; set; }
    public double Lon { get; set; }

    public string Source { get; set; } = "";
}

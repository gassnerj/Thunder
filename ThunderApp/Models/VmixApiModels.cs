using System;
using System.Collections.Generic;

namespace ThunderApp.Models;

public sealed class VmixWarningDto
{
    public string Id { get; set; } = "";
    public string Event { get; set; } = "";
    public string Headline { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Urgency { get; set; } = "";
    public string AreaDescription { get; set; } = "";
    public DateTimeOffset? Effective { get; set; }
    public DateTimeOffset? Expires { get; set; }
    public DateTimeOffset? Ends { get; set; }
    public DateTimeOffset? Onset { get; set; }
}

public sealed class VmixObservationDto
{
    public string StationId { get; set; } = "";
    public string StationName { get; set; } = "";
    public string TimeZone { get; set; } = "";
    public DateTime TimestampUtc { get; set; }

    public double? TemperatureF { get; set; }
    public double? DewPointF { get; set; }
    public double? RelativeHumidity { get; set; }
    public double? WindMph { get; set; }
    public string WindDirection { get; set; } = "";
    public double? HeatIndexF { get; set; }
    public double? WindChillF { get; set; }
    public double? BarometricPressureInHg { get; set; }
    public double? SeaLevelPressureInHg { get; set; }
}


public sealed class VmixSnapshotRowDto
{
    public DateTime GeneratedAtUtc { get; set; }
    public string SourceRequested { get; set; } = "";
    public string SourceActive { get; set; } = "";

    public bool UseRadiusFilter { get; set; }
    public double RadiusMiles { get; set; }
    public double? CenterLat { get; set; }
    public double? CenterLon { get; set; }

    public int WarningCount { get; set; }

    public string StationId { get; set; } = "";
    public string StationName { get; set; } = "";
    public string TimeZone { get; set; } = "";
    public DateTime? ObservationTimestampUtc { get; set; }

    public double? TemperatureF { get; set; }
    public double? DewPointF { get; set; }
    public double? RelativeHumidity { get; set; }
    public double? WindMph { get; set; }
    public string WindDirection { get; set; } = "";
    public double? HeatIndexF { get; set; }
    public double? WindChillF { get; set; }
    public double? BarometricPressureInHg { get; set; }
    public double? SeaLevelPressureInHg { get; set; }
}


public sealed class VmixLocationRowDto
{
    public DateTime generatedAtUtc { get; set; }
    public string locLine { get; set; } = "";
    public string locDetail { get; set; } = "";
    public string road { get; set; } = "";
    public string city { get; set; } = "";
    public string state { get; set; } = "";
    public double distMi { get; set; }
    public string dir { get; set; } = "";
    public double lat { get; set; }
    public double lon { get; set; }
    public string source { get; set; } = "";
}

public sealed class VmixApiSnapshot
{
    public DateTime generatedAtUtc { get; set; }
    public bool useRadiusFilter { get; set; }
    public double radiusMiles { get; set; }
    public double? centerLat { get; set; }
    public double? centerLon { get; set; }

    public string sourceRequested { get; set; } = "NearestAsos";
    public string sourceActive { get; set; } = "NearestAsos";
    public bool vehicleStationAvailable { get; set; }

    public VmixObservationDto? observation { get; set; }
    public IReadOnlyList<VmixWarningDto> warnings { get; set; } = [];
    public IReadOnlyList<VmixSnapshotRowDto> rows { get; set; } = [];
    public IReadOnlyList<VmixLocationRowDto> locationRows { get; set; } = [];
}

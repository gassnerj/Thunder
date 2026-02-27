#nullable enable
using System.Collections.Generic;

namespace GeoJsonWeather.Models;

public sealed class StationObservationSnapshot
{
    public ObservationModel? Observation { get; init; }
    public ObservationStationModel? ActiveStation { get; init; }
    public IReadOnlyList<ObservationStationModel> Stations { get; init; } = [];
    public IReadOnlyDictionary<string, ObservationModel?> StationObservations { get; init; } = new Dictionary<string, ObservationModel?>();
}

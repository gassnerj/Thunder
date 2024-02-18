#nullable enable
namespace GeoJsonWeather.Models;

public class ObservationStationModel
{
    public string? Id { get; set; }
    public Coordinate Coordinates { get; set; }
    public string? StationIdentifier { get; set; }
    public string? Name { get; set; }
    public string? TimeZone { get; set; }
}
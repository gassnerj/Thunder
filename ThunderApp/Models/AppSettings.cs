namespace ThunderApp.Models;

public sealed class AppSettings
{
    public MapboxSettings Mapbox { get; set; } = new();
    public NominatimSettings Nominatim { get; set; } = new();
}

public sealed class MapboxSettings
{
    public string AccessToken { get; set; } = "";
}

public sealed class NominatimSettings
{
    public string UserAgent { get; set; } = "ThunderApp/1.0 (contact: thunderapp@users.noreply.github.com)";
    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";
}

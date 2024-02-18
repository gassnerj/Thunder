using System.Collections.Generic;
using System.Threading.Tasks;
using GeoJsonWeather.Api;
using GeoJsonWeather.Stations;

namespace GeoJsonWeather.Models;

public class ForecastZoneModel : IApiRetreivable
{
    private readonly string _url;

    public string Id { get; set; }
    public string Name { get; set; }
    public string State { get; set; }
    public string CWA { get; set; }
    public string ForecastOfficeUrl { get; set; }
    public string TimeZone { get; set; }
    public List<string> ObservationStationUrls { get; set; }

    public ForecastZoneModel()
    {
        ObservationStationUrls = new List<string>();
    }

    public ForecastZoneModel(string url)
    {
        _url                   = url;
        ObservationStationUrls = new List<string>();
    }

    public async Task<string> GetData()
    {
        ApiFetcher apiFetcher = new ApiFetcherBuilder(_url).Build();
        return await apiFetcher.FetchData();
    }
}
#nullable enable
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using GeoJsonWeather.Api;

namespace GeoJsonWeather.Stations;

public class WeatherApiRetriever : IApiRetreivable
{
    private readonly string? _url;

    public WeatherApiRetriever(string? url)
    {
        _url = url;
    }

    public async Task<string> GetData()
    {
        ApiFetcher apiFetcher = new ApiFetcherBuilder(_url).Build();
        return await apiFetcher.FetchData();
    }
}
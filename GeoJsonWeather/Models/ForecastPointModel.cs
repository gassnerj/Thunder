using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeoJsonWeather.Api;
using GeoJsonWeather.Stations;

namespace GeoJsonWeather.Models;

public class ForecastPointModel : IApiRetreivable
{
    private readonly string _url;
    
    public string CWA { get; set; }
    public string ForecastOfficeUrl { get; set; }
    public string GridId { get; set; }
    public string GridX { get; set; }
    public string GridY { get; set; }
    public string ZoneUrl { get; init; }
    public string CountyUrl { get; set; }
    public string FireWeatherZone { get; set; }
    public string Zone
    {
        get
        {
            Match match = Regex.Match(ZoneUrl, @"[A-Z]{3}[0-9]{3}");
            return match.Success ? match.Groups[0].Value : string.Empty;
        }
    }

    public ForecastPointModel(string url)
    {
        _url = url;
    }

    internal protected ForecastPointModel()
    {
        
    }

    public async Task<string> GetData()
    {
        ApiFetcher apiFetcher = new ApiFetcherBuilder(_url).Build();
        return await apiFetcher.FetchData();
    }
}
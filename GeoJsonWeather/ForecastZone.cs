using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeoJsonWeather.Stations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonException = System.Text.Json.JsonException;

namespace GeoJsonWeather;


public class ForecastZone : IApiRetreivable
{
    private string _url;
    
    public string ZoneUrl { get; set; }
    public string CountyUrl { get; set; }
    public string FireWeatherZone { get; set; }

    public ForecastZone(string url)
    {
        _url = url;
    }
    
    public async Task<string> GetFromApi()
    {
        ApiFetcher apiFetcher = new ApiFetcherBuilder(_url).Build();
        return await apiFetcher.FetchData();
    }
}

public class ForecastZoneJsonParser : IJsonParser<ForecastZone>
{

    private readonly string _jsonString;
    
    public ForecastZoneJsonParser(string jsonString)
    {
        
        _jsonString = jsonString;
        

    }

    public IEnumerable<T> GetItems<T>()
    {
        var forecastZones = new List<ForecastZone>();
        
        try
        {
            var jsonObject = JsonConvert.DeserializeObject<JObject>(_jsonString);
            
            forecastZones.AddRange((jsonObject?["features"] ?? throw new InvalidOperationException()).Select(ParseNode));
        }
        catch (JsonException ex)
        {
            Console.WriteLine(ex.Message);
        }

        return (IEnumerable<T>)forecastZones;
    }

    private ForecastZone   ParseNode(JToken stationNode)
    {
        throw new NotImplementedException();
    }
}
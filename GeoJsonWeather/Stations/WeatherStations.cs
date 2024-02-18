#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonException = System.Text.Json.JsonException;

namespace GeoJsonWeather.Stations;

public interface IApiRetreivable
{
    Task<string> GetFromApi();
}

public class WeatherStations : IApiRetreivable
{
    private readonly string? _url;
    
    public WeatherStations(string? url)
    {
        _url = url;
    }

    public async Task<string> GetFromApi()
    {
        // "https: //api.weather.gov/stations"
        
        ApiFetcher apiFetcher   = new ApiFetcherBuilder(_url).Build();
        return await apiFetcher.FetchData();
    }
    

}

public interface IJsonParser<T>
{
    IEnumerable<T> GetItems<T>();
}

public class WeatherStationsJsonParser: IJsonParser<WeatherStation>
{
    private readonly string _jsonString;
    
    public WeatherStationsJsonParser(string jsonString)
    {
        _jsonString = jsonString;
    }
    
    public IEnumerable<T> GetItems<T>()
    {
        var weatherStations = new List<WeatherStation>();

        try
        {
            var jsonObject = JsonConvert.DeserializeObject<JObject>(_jsonString);
            
            weatherStations.AddRange((jsonObject?["features"] ?? throw new InvalidOperationException()).Select(ParseNode));
        }
        catch (JsonException ex)
        {
            Console.WriteLine(ex.Message);
        }

        return (IEnumerable<T>)weatherStations;
    }

    private WeatherStation ParseNode(JToken stationNode)
    {
        var stationId   = stationNode["id"]?.ToString();
        var stationType = stationNode["type"]?.ToString();
    
        JToken geometry    = stationNode["geometry"];
        var    coordinates = geometry?["coordinates"]?.ToObject<List<double>>();
    
        JToken properties        = stationNode["properties"];
        var    elevation         = properties?["elevation"]?["value"]?.ToObject<double>();
        var    stationIdentifier = properties?["stationIdentifier"]?.ToString();
        var    name              = properties?["name"]?.ToString();
        var    timeZone          = properties?["timeZone"]?.ToString();
        var    forecast          = properties?["forecast"]?.ToString();
        var    county            = properties?["county"]?.ToString();
        var    fireWeatherZone   = properties?["fireWeatherZone"]?.ToString();

        return new WeatherStation
        {
            Id   = stationId,
            Type = stationType,
            Geometry = new Geometry
            {
                Type        = geometry?["type"]?.ToString(),
                Coordinates = coordinates
            },
            Properties = new Properties
            {
                Id   = properties?["@id"]?.ToString(),
                Type = properties?["@type"]?.ToString(),
                Elevation = new Elevation
                {
                    UnitCode = properties?["elevation"]?["unitCode"]?.ToString(),
                    Value    = elevation.GetValueOrDefault()
                },
                StationIdentifier = stationIdentifier,
                Name              = name,
                TimeZone          = timeZone,
                Forecast          = forecast,
                County            = county,
                FireWeatherZone   = fireWeatherZone
            }
        };
    }
}

public class WeatherStation
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public Geometry? Geometry { get; set; }
    public Properties? Properties { get; set; }
}

public class Geometry
{
    public string? Type { get; set; }
    public List<double>? Coordinates { get; set; }
}

public class Properties
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public Elevation? Elevation { get; set; }
    public string? StationIdentifier { get; set; }
    public string? Name { get; set; }
    public string? TimeZone { get; set; }
    public string? Forecast { get; set; }
    public string? County { get; set; }
    public string? FireWeatherZone { get; set; }
}

public class Elevation
{
    public string? UnitCode { get; set; }
    public double Value { get; set; }
}
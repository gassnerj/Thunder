using System;
using System.Linq;
using GeoJsonWeather.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonException = System.Text.Json.JsonException;

namespace GeoJsonWeather.Parsers;

public class ForecastZoneParser : IJsonParser<ForecastZoneModel>
{
    private readonly string _jsonString;

    public ForecastZoneParser(string jsonString)
    {
        _jsonString = jsonString;
    }

    public ForecastZoneModel GetItem()
    {
        try
        {
            var jsonObject = JsonConvert.DeserializeObject<JObject>(_jsonString);
            var stations   = jsonObject["properties"]?["observationStations"]?.Select(item => Extensions.Value<string>(item)).ToList();

            return new ForecastZoneModel()
            {
                Id                     = jsonObject?["properties"]?["id"]?.Value<string>(),
                Name                   = jsonObject?["properties"]?["name"]?.Value<string>(),
                State                  = jsonObject?["properties"]?["state"]?.Value<string>(),
                CWA                    = jsonObject?["properties"]?["cwa"]?[0]?.Value<string>(),
                ForecastOfficeUrl      = jsonObject?["properties"]?["forecastOffices"]?[0]?.Value<string>(),
                TimeZone               = jsonObject?["properties"]?["timeZone"]?[0]?.Value<string>(),
                ObservationStationUrls = stations
            };
        }
        catch (JsonException ex)
        {
            Console.WriteLine(ex.Message);
        }
        return null;
    }
}
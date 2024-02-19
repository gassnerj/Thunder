using System;
using GeoJsonWeather.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonException = System.Text.Json.JsonException;

namespace GeoJsonWeather.Parsers;

public class ForecastPointParser : IJsonParser<ForecastPointModel>
{
    public ForecastPointModel GetItem(string jsonString)
    {
        try
        {
            var jsonObject = JsonConvert.DeserializeObject<JObject>(jsonString);

            return new ForecastPointModel()
            {
                CWA               = jsonObject?["properties"]?["cwa"]?.Value<string>(),
                ForecastOfficeUrl = jsonObject?["properties"]?["forecastOffice"]?.Value<string>(),
                GridX             = jsonObject?["properties"]?["gridX"]?.Value<string>(),
                GridY             = jsonObject?["properties"]?["gridY"]?.Value<string>(),
                ZoneUrl           = jsonObject?["properties"]?["forecastZone"]?.Value<string>(),
                FireWeatherZone   = jsonObject?["properties"]?["fireWeatherZone"]?.Value<string>(),
                CountyUrl         = jsonObject?["properties"]?["county"]?.Value<string>()
            };
        }
        catch (JsonException ex)
        {
            Console.WriteLine(ex.Message);
        }
        return null;
    }
}
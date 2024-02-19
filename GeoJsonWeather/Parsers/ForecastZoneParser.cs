using System;
using System.Collections.Generic;
using System.Linq;
using GeoJsonWeather.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonException = System.Text.Json.JsonException;

namespace GeoJsonWeather.Parsers;

public class ForecastZoneParser : IJsonParser<ForecastZoneModel>
{
    public ForecastZoneModel GetItem(string jsonString)
    {
        try
        {
            var jsonObject = JsonConvert.DeserializeObject<JObject>(jsonString);
            var stations   = jsonObject["properties"]?["observationStations"]?.Select(item => item.Value<string>()).ToList();

            return new ForecastZoneModel()
            {
                Id                     = jsonObject?["properties"]?["id"]?.Value<string>(),
                Name                   = jsonObject?["properties"]?["name"]?.Value<string>(),
                State                  = jsonObject?["properties"]?["state"]?.Value<string>(),
                CWA                    = jsonObject?["properties"]?["cwa"]?[0]?.Value<string>(),
                ForecastOfficeUrl      = jsonObject?["properties"]?["forecastOffices"]?[0]?.Value<string>(),
                TimeZone               = jsonObject?["properties"]?["timeZone"]?[0]?.Value<string>(),
                ZonePolygonCoordinates = ParseCoordinates(jsonObject["geometry"]?["coordinates"]?[0]),
                ObservationStationUrls = stations
            };
        }
        catch (JsonException ex)
        {
            Console.WriteLine(ex.Message);
        }
        return null;
    }

    private List<Coordinate> ParseCoordinates(JToken jToken)
    {
        var coordinates = new List<Coordinate>();
        
        foreach (JToken coordinate in jToken)
        {
            var latitude  = Convert.ToDouble(coordinate[0]);
            var longitude = Convert.ToDouble(coordinate[1]);
            coordinates.Add(new Coordinate(latitude, longitude));            
        }
        return coordinates;
    }
}
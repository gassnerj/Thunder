using System;
using GeoJsonWeather.Models;
using GeoJsonWeather.Stations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonException = System.Text.Json.JsonException;

namespace GeoJsonWeather.Parsers;

public class ObservationStationParser : IJsonParser<ObservationStationModel>
{
    private readonly string _jsonString;

    public ObservationStationParser(string jsonString)
    {
        _jsonString = jsonString;
    }

    public ObservationStationModel GetItem()
    {
        try
        {
            var jsonObject = JsonConvert.DeserializeObject<JObject>(_jsonString);

            return new ObservationStationModel
            {
                Id = jsonObject?["id"]?.ToString(),
                Coordinates = new Coordinate(jsonObject?["geometry"]?["coordinates"]?[1]?.Value<string>(),
                                             jsonObject?["geometry"]?["coordinates"]?[0]?.Value<string>()),
                Name              = jsonObject?["properties"]?["name"]?.Value<string>(),
                StationIdentifier = jsonObject?["properties"]?["stationIdentifier"]?.Value<string>(),
                TimeZone          = jsonObject?["properties"]?["timeZone"]?.Value<string>(),
            };
        }
        catch (JsonException ex)
        {
            Console.WriteLine(ex.Message);
        }

        return null;
    }
}
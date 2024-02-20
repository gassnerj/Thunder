using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GeoJsonWeather.Models;

namespace GeoJsonWeather.Parsers
{
    public class ForecastZoneParser : IJsonParser<ForecastZoneModel>
    {
        public ForecastZoneModel GetItem(JsonElement jsonElement)
        {
            try
            {
                // Extracting observation stations
                var stations = jsonElement.GetProperty("properties").GetProperty("observationStations")
                    .EnumerateArray()
                    .Select(item => item.GetString())
                    .ToList();

                return new ForecastZoneModel
                {
                    Id                     = jsonElement.GetProperty("properties").GetProperty("id").GetString(),
                    Name                   = jsonElement.GetProperty("properties").GetProperty("name").GetString(),
                    State                  = jsonElement.GetProperty("properties").GetProperty("state").GetString(),
                    CWA                    = jsonElement.GetProperty("properties").GetProperty("cwa")[0].GetString(),
                    ForecastOfficeUrl      = jsonElement.GetProperty("properties").GetProperty("forecastOffices")[0].GetString(),
                    TimeZone               = jsonElement.GetProperty("properties").GetProperty("timeZone")[0].GetString(),
                    ZonePolygonCoordinates = ParseCoordinates(jsonElement.GetProperty("geometry").GetProperty("coordinates")[0]),
                    ObservationStationUrls = stations
                };
            }
            catch (JsonException ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        private static List<Coordinate> ParseCoordinates(JsonElement jsonElement)
        {
            var coordinates = new List<Coordinate>();

            foreach (JsonElement coordinate in jsonElement.EnumerateArray())
            {
                double latitude  = coordinate[0].GetDouble();
                double longitude = coordinate[1].GetDouble();
                coordinates.Add(new Coordinate(latitude, longitude));
            }
            return coordinates;
        }
    }
}

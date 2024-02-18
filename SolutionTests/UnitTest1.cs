using System.Security.Principal;
using GeoJsonWeather.Stations;
using GeoJsonWeather;
using MeteorologyCore;

namespace SolutionTests;

public class UnitTest1
{
    [Fact]
    public void StationsNotEmptyTest()
    {
        const string URL = "https://api.weather.gov/points/33.9595,-98.6812";

        IApiRetreivable forecastZone = new ForecastZone(URL);
        string          zoneJson     = forecastZone.GetFromApi().Result;

        IJsonParser<ForecastZone> zoneParser = new ForecastZoneJsonParser(zoneJson);

        // get zone url from zone api

        IApiRetreivable stations = new WeatherStations(@"https://api.weather.gov/stations");
        string          json     = stations.GetFromApi().Result;

        IJsonParser<WeatherStation> stationParser   = new WeatherStationsJsonParser(json);
        var                         weatherStations = stationParser.GetItems<WeatherStation>();


        Assert.NotEmpty(weatherStations);
    }
}
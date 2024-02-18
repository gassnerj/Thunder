using System.Globalization;
using GeoJsonWeather.Stations;
using GeoJsonWeather.Models;
using GeoJsonWeather.Parsers;
using MeteorologyCore;
using Xunit.Abstractions;

namespace SolutionTests;

public class UnitTest1
{
    private readonly ITestOutputHelper _testOutputHelper;

    public UnitTest1(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void ForecastPointTest()
    {
        const string URL = "https://api.weather.gov/points/33.9595,-98.6812";

        IApiRetreivable                  forecastPoint = new WeatherApiRetriever(URL);
        string                           pointJson     = forecastPoint.GetData().Result;
        IJsonParser<ForecastPointModel> jsonParser    = new ForecastPointParser(pointJson);
        ForecastPointModel                    pointModel         = jsonParser.GetItem();

        Assert.NotNull(pointModel);
    }

    [Fact]
    public void ForecastZoneTest()
    {
        const string URL = "https://api.weather.gov/zones/forecast/TXZ086";

        IApiRetreivable                 forecastZone = new WeatherApiRetriever(URL);
        string                          zoneJson     = forecastZone.GetData().Result;
        IJsonParser<ForecastZoneModel> zoneParser   = new ForecastZoneParser(zoneJson);
        ForecastZoneModel                    zoneModel         = zoneParser.GetItem();

        Assert.NotNull(zoneModel);
    }

    [Fact]
    public void ObservationStationTest()
    {
        const string URL = "https://api.weather.gov/stations/KFDR";

        IApiRetreivable                       api           = new WeatherApiRetriever(URL);
        string                                json               = api.GetData().Result;
        IJsonParser<ObservationStationModel> stationParser      = new ObservationStationParser(json);
        ObservationStationModel?                   observationStation = stationParser.GetItem();

        Assert.NotNull(observationStation);
    }

    [Fact]
    public void ObservationTest()
    {
        const string URL = "https://api.weather.gov/stations/KFDR/observations/latest";
        
        IApiRetreivable               api                = new WeatherApiRetriever(URL);
        string                        json               = api.GetData().Result;
        IJsonParser<ObservationModel> stationParser      = new ObservationParser(json);
        ObservationModel?             observationStation = stationParser.GetItem();

        _testOutputHelper.WriteLine(observationStation.Temperature.ToFahrenheit().ToString());
        _testOutputHelper.WriteLine(observationStation.DewPoint.ToFahrenheit().ToString());
        _testOutputHelper.WriteLine(observationStation.WindChill.ToFahrenheit().ToString());
        _testOutputHelper.WriteLine(RelativeHumidityCalculator.ToString(observationStation.RelativeHumidity));
        
        Assert.NotNull(observationStation);
    }
}
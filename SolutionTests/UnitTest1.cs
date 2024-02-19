using System.Globalization;
using GeoJsonWeather;
using GeoJsonWeather.Api;
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

        var                 apiFetcher = new ApiFetcher(string.Empty, URL);
        var                 apiManager = new ApiManager(apiFetcher);
        var                 apiParser  = new ForecastPointParser();
        ForecastPointModel? model      = apiManager.GetModel(apiParser);

        Assert.NotNull(model);
    }

    [Fact]
    public void ForecastZoneTest()
    {
        const string URL = "https://api.weather.gov/zones/forecast/TXZ086";

        var                 apiFetcher = new ApiFetcher(string.Empty, URL);
        var                 apiManager = new ApiManager(apiFetcher);
        var                 apiParser  = new ForecastZoneParser();
        ForecastZoneModel? model      = apiManager.GetModel(apiParser);

        Assert.NotNull(model);
    }

    [Fact]
    public void ObservationStationTest()
    {
        const string URL = "https://api.weather.gov/stations/KFDR";

        var                apiFetcher = new ApiFetcher(string.Empty, URL);
        var                apiManager = new ApiManager(apiFetcher);
        var                apiParser  = new ObservationStationParser();
        ObservationStationModel? model      = apiManager.GetModel(apiParser);

        Assert.NotNull(model);
    }

    [Fact]
    public void ObservationTest()
    {
        const string URL = "https://api.weather.gov/stations/KFDR/observations/latest";

        var                apiFetcher = new ApiFetcher(string.Empty, URL);
        var                apiManager = new ApiManager(apiFetcher);
        var                apiParser  = new ObservationParser();
        ObservationModel? model      = apiManager.GetModel(apiParser);

        

        _testOutputHelper.WriteLine(model.Temperature.ToFahrenheit().ToString());
        _testOutputHelper.WriteLine(model.DewPoint.ToFahrenheit().ToString());
        _testOutputHelper.WriteLine(model.WindChill?.ToFahrenheit()?.ToString() ?? "");
        _testOutputHelper.WriteLine(RelativeHumidityCalculator.ToString(model.RelativeHumidity));

        Assert.NotNull(model);
    }

    [Fact]
    public void ObservationManagerTest()
    {
        // ObservationModel? model = ObservationManager.GetNearestObservations(33.9595,-98.6812);
        // Assert.NotNull(model);
        //
        // if (model is null)
        //     return;
        //
        // _testOutputHelper.WriteLine("Air Temp: " + model.Temperature.ToFahrenheit());
        // _testOutputHelper.WriteLine("Dewpoint: " + model.DewPoint.ToFahrenheit());
        // _testOutputHelper.WriteLine("Wind Chill :" + model.WindChill?.ToFahrenheit());
        // _testOutputHelper.WriteLine("Humidity: " + RelativeHumidityCalculator.ToString(model.RelativeHumidity));
    }
}
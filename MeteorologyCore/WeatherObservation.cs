using System.Diagnostics;
using GeoJsonWeather;
using GeoJsonWeather.Api;
using Newtonsoft.Json.Linq;

namespace MeteorologyCore;

public class WeatherObservation
{
    private readonly IHumidityCalculator _humidityCalculator = null!;
    private readonly IDewPointCalculator _dewPointCalculator = null!;
    private readonly IHeatIndexCalculator _heatIndexCalculator = null!;
    private readonly IWindChillCalculator _windChillCalculator = null!;

    public Fahrenheit Temperature { get; set; } = null!;
    public HeatIndex HeatIndex { get; set; } = null!;
    public DewPoint DewPoint { get; set; } = null!;
    public RelativeHumidity RH { get; set; } = null!;
    public Wind Wind { get; set; } = null!;
    public WindChill WindChill { get; set; } = null!;
    public Pressure Pressure { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public IASOSStation SelectedStation { get; }

    private WeatherObservation(IASOSStation selectedStation)
    {
    }

    public WeatherObservation(Fahrenheit t,                   Pressure             p, Wind w, RelativeHumidity rh,
        IHumidityCalculator              humidityCalculator,  IDewPointCalculator  dewPointCalculator,
        IHeatIndexCalculator             heatIndexCalculator, IWindChillCalculator windChillCalculator, IASOSStation selectedStation)
    {
        Temperature = t;
        Pressure    = p;
        Wind        = w;
        RH          = rh;

        _humidityCalculator  = humidityCalculator;
        _dewPointCalculator  = dewPointCalculator;
        _heatIndexCalculator = heatIndexCalculator;
        _windChillCalculator = windChillCalculator;
        SelectedStation      = selectedStation;

        HeatIndex = heatIndexCalculator.Calculate(t, rh);
        DewPoint  = dewPointCalculator.Calculate(t.ToCelsius(), rh, p);
        WindChill = windChillCalculator.Calculate(t, w.Speed);

        dewPointCalculator.Calculate(Temperature.ToCelsius(), RH, Pressure);
    }

    //public static List<WeatherObservation> ReadMetarFromCSV(FileInfo file)
    //{
    //    var obsList = new List<WeatherObservation>();

    //    string[] rawMetar = File.ReadAllLines(file.FullName);

    //    foreach (string m in rawMetar)
    //    {
    //        DateTime ts;
    //        try
    //        {
    //            string metar = m.Split(',')[2];
    //            if (m.Length > 30)
    //            {
    //                //var d = MetarDecoder.ParseWithMode(metar);
    //                //var ws = new WeatherObservation(
    //                //    new Celsius(d.AirTemperature.ActualValue).ToFahrenheit(),
    //                //    new Pressure(d.Pressure.ActualValue),
    //                //    new Wind()
    //                //    {
    //                //        Speed = d.SurfaceWind.MeanSpeed.ActualValue,
    //                //        Direction = new Direction(d.SurfaceWind.MeanDirection.ActualValue)
    //                //    },
    //                //    new RelativeHumidity(52))
    //                {
    //                    Timestamp = new DateTime(DateTime.Now.Year, DateTime.Now.Month, Convert.ToInt32(d.Day.Value), Convert.ToInt32(d.Time.Replace("UTC", "").Substring(0, 2)), Convert.ToInt32(d.Time.Replace("UTC", "").Substring(3, 2)), 0)
    //                };
    //                ws.Timestamp = ws.Timestamp.ToLocalTime();
    //                obsList.Add(ws);


    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Debug.Print(ex.Message);
    //        }
    //    }
    //    return obsList;
    //}

    public WeatherObservation FromJToken(JToken o)
    {
        double? temperatureValue = ConvertToDouble(o["properties"]?["temperature"]?["value"]?.ToString());
        Celsius cTemp            = temperatureValue ?? 0;
        Temperature = cTemp.ToFahrenheit();

        double? dewpointValue = ConvertToDouble(o["properties"]?["dewpoint"]?["value"]?.ToString());
        Celsius dTemp         = dewpointValue ?? 0;
        DewPoint = new DewPoint(dTemp.ToFahrenheit());

        Direction? windDirection = GetWindDirection(o);
        double     windSpeed     = (GetWindSpeed(o) ?? 0);
        Wind = new Wind(direction: windDirection, speed: windSpeed);

        RH       = new RelativeHumidity((GetPropertyValue<Celsius>(o, "relativeHumidity", "value") ?? 0));
        Pressure = new Pressure((GetPropertyValue<Celsius>(o, "barometricPressure", "value") ?? 0) / 100);

        Timestamp = GetPropertyValue<DateTime>(o, "timestamp");

        WindChill = WindChill.Calculate(Temperature, Wind.Speed);

        return this;
    }

    private static double? ConvertToDouble(string? value)
    {
        return double.TryParse(value, out double parsedValue) ? parsedValue : null;
    }

    private static T? GetPropertyValue<T>(JToken token, string property, string? subProperty = null)
    {
        JToken? valueToken = token["properties"]?[property]?["value"];
        if (valueToken == null) return default;
        if (subProperty != null)
        {
            valueToken = valueToken[subProperty];
        }

        return valueToken != null ? valueToken.ToObject<T>() : default;
    }

    private static Direction? GetWindDirection(JToken o)
    {
        var windDirectionValue = o["properties"]?["windDirection"]?["value"]?.ToString();
        return string.IsNullOrEmpty(windDirectionValue) ? null : new Direction(ConvertToDouble(windDirectionValue) ?? 0);
    }

    private static double? GetWindSpeed(JToken o)
    {
        var windSpeedValue = o["properties"]?["windSpeed"]?["value"]?.ToString();
        if (string.IsNullOrEmpty(windSpeedValue))
        {
            return 0;
        }
        return ConvertToDouble(windSpeedValue) * 2.237;
    }

    public async void GetObs(string stationId)
    {
        var          url          = $"https://api.weather.gov/stations/{stationId}/observations/latest";
        const string USER_AGENT   = "GeoJsonWeather/1.0 (Windows; U; Windows NT 5.1; en-US; rv:1.8.1.6) Gecko/20070725 Firefox/2.0.0.6";
        ApiFetcher   apiFetcher   = new ApiFetcherBuilder(url).Build();
        string       jsonResponse = await apiFetcher.FetchData();

        /*JToken  o     = FetchData($"https://api.weather.gov/stations/{0}/observations/current", SelectedStation, MyFeatureCollection.GetFeature);
        Celsius cTemp = Convert.ToDouble(o["properties"]["temperature"]["value"].ToString());
        Temperature = cTemp.ToFahrenheit();
        Celsius dTemp = Convert.ToDouble(o["properties"]["dewpoint"]["value"].ToString());
        DewPoint = new DewPoint(dTemp.ToFahrenheit());
        Wind = new Wind
        {
            Speed     = (Celsius)Convert.ToDouble(o["properties"]["windSpeed"]["value"].ToString()) * 2.237,
            Direction = new Direction((Celsius)Convert.ToDouble(o["properties"]["windDirection"]["value"].ToString()))
        };
        RH        = new RelativeHumidity((Celsius)Convert.ToDouble(o["properties"]["relativeHumidity"]["value"].ToString()));
        Pressure  = new Pressure((Celsius)Convert.ToDouble(o["properties"]["barometricPressure"]["value"].ToString()) / 100);
        Timestamp = DateTime.Parse(o["properties"]["timestamp"].ToString());
        WindChill = WindChill.Calculate(Temperature, Wind.Speed);*/
    }
}
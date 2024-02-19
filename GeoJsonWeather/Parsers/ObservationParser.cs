using System;
using GeoJsonWeather.Models;
using MeteorologyCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeoJsonWeather.Parsers
{
    public class ObservationParser : IJsonParser<ObservationModel>
    {
        public ObservationModel GetItem(string jsonString)
        {
            var jsonObject = JsonConvert.DeserializeObject<JObject>(jsonString);

            if (jsonObject is null)
                return null;

            double       temperatureValue         = ToDouble(jsonObject["properties"]?["temperature"]?["value"]?.ToString());
            double       dewPointTemperatureValue = ToDouble(jsonObject["properties"]?["dewpoint"]?["value"]?.ToString());
            var          windDirectionValue       = jsonObject["properties"]?["windDirection"]?["value"]?.ToString();
            double       windSpeed                = ToDouble(jsonObject["properties"]?["windSpeed"]?["value"]?.ToString());
            double       barometricPressure       = ToDouble(jsonObject["properties"]?["barometricPressure"]?["value"]?.ToString());
            double       seaLevelPressure         = ToDouble(jsonObject["properties"]?["seaLevelPressure"]?["value"]?.ToString());
            var          windChill                = jsonObject["properties"]?["windChill"]?["value"]?.ToString();
            var          heatIndex                = jsonObject["properties"]?["heatIndex"]?["value"]?.ToString();
            double       relativeHumidity         = ToDouble(jsonObject["properties"]?["relativeHumidity"]?["value"]?.ToString());
            var          timeStamp                = jsonObject["properties"]?["timestamp"]?.ToString();
            var          windDirection            = new Direction(ToDouble(windDirectionValue));
            ITemperature airTemp                  = new Celsius(temperatureValue);
            ITemperature dewPointTemp             = new Celsius(dewPointTemperatureValue);
            ITemperature windChillTemp            = windChill == string.Empty ? null : new Celsius(ToDouble(windChill));
            ITemperature heatIndexTemp            = heatIndex == string.Empty ? null : new Celsius(ToDouble(heatIndex));
            var          baroPressure             = new Pressure(barometricPressure);
            var          seaPressure              = new Pressure(seaLevelPressure);
            var          wind                     = new Wind(windDirection, windSpeed * 2.237);


            return new ObservationModel
            {
                Temperature        = airTemp,
                HeatIndex          = heatIndexTemp,
                DewPoint           = dewPointTemp,
                RelativeHumidity   = relativeHumidity,
                Wind               = wind,
                WindChill          = windChillTemp,
                BarometricPressure = baroPressure,
                SeaLevelPressure   = seaPressure,
                Timestamp          = FeatureCollection.ISO8601Parse(timeStamp)
            };
        }

        private double ToDouble(string value)
        {
            return string.IsNullOrEmpty(value) ? 0.0 : Convert.ToDouble(value);
        }
    }
}
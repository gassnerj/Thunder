using System;
using GeoJsonWeather.Models;
using MeteorologyCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeoJsonWeather.Parsers
{
    public class ObservationParser : IJsonParser<ObservationModel>
    {
        private readonly string _jsonString;

        public ObservationParser(string jsonString)
        {
            _jsonString = jsonString;
        }

        public ObservationModel GetItem()
        {
            var jsonObject = JsonConvert.DeserializeObject<JObject>(_jsonString);

            var          airTemperature      = Convert.ToDouble(jsonObject["properties"]?["temperature"]?["value"]?.ToString());
            var          dewPointTemperature = Convert.ToDouble(jsonObject["properties"]?["dewpoint"]?["value"]?.ToString());
            var          windDirectionValue  = jsonObject["properties"]?["windDirection"]?["value"]?.ToString();
            var          windSpeed           = Convert.ToDouble(jsonObject["properties"]?["windSpeed"]?["value"]?.ToString());
            var          barometricPressure  = Convert.ToDouble(jsonObject["properties"]?["barometricPressure"]?["value"]?.ToString());
            var          seaLevelPressure    = Convert.ToDouble(jsonObject["properties"]?["seaLevelPressure"]?["value"]?.ToString());
            var          windChill           = jsonObject["properties"]?["windChill"]?["value"]?.ToString();
            var          heatIndex           = jsonObject["properties"]?["heatIndex"]?["value"]?.ToString();
            var          relativeHumidity    = Convert.ToDouble(jsonObject["properties"]?["relativeHumidity"]?["value"]?.ToString());
            var          timeStamp           = jsonObject["properties"]?["timestamp"]?.ToString();
            var          windDirection       = new Direction(Convert.ToDouble(windDirectionValue));
            ITemperature airTemp             = new Celsius(airTemperature);
            ITemperature dewPointTemp        = new Celsius(dewPointTemperature);
            ITemperature windChillTemp       = windChill == string.Empty ? null : new Celsius(Convert.ToDouble(windChill));
            ITemperature heatIndexTemp       = heatIndex == string.Empty ? null : new Celsius(Convert.ToDouble(heatIndex));
            var          baroPressure        = new Pressure(barometricPressure);
            var          seaPressure         = new Pressure(seaLevelPressure);
            var          wind                = new Wind(windDirection, windSpeed * 2.237);

            return new ObservationModel
            {
                Temperature = airTemp,
                HeatIndex = heatIndexTemp,
                DewPoint = dewPointTemp,
                RelativeHumidity = relativeHumidity,
                Wind = wind,
                WindChill = windChillTemp,
                BarometricPressure = baroPressure,
                SeaLevelPressure = seaPressure,
                Timestamp = FeatureCollection.ISO8601Parse(timeStamp)
            };
        }
    }
}

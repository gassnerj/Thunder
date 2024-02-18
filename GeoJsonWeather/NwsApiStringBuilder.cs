using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoJsonWeather
{
    public class NwsApiStringBuilder
    {
        private readonly string _baseString = "https://api.weather.gov/";
        private readonly string _alertsString = "alerts/active";

        public string GetAll()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(_baseString);
            stringBuilder.Append(_alertsString);
            return stringBuilder.ToString();
        }

        public string GetByState(string stateAbbreviation)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(_baseString);
            stringBuilder.Append(_alertsString);
            stringBuilder.Append($"?area={stateAbbreviation}");
            return stringBuilder.ToString();
        }

        public string GetByLatLon(double lat, double lon)
        {
            //https://api.weather.gov/alerts?point=38.9807,-76.9373

            var stringBuilder = new StringBuilder();
            stringBuilder.Append(_baseString);
            stringBuilder.Append(_alertsString);
            stringBuilder.Append($"?point={lat},{lon}");
            return stringBuilder.ToString();
        }
    }
}

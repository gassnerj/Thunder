using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using System.Text;
using System;

namespace GPSData
{
    public class GeoCode
    {
        private string _url = string.Empty;
        private readonly string _apiKey = string.Empty;
        public bool GetAPI { get; set; } = false;

        public GeoCode(string apiKey)
        {
            _apiKey = apiKey;
        }

        public IGeoCodeLocation? GetData(INMEA nmea)
        {
            _url = $@"https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={nmea.Latitude}&lon={nmea.Longitude}&jsonv2";
            //_url = $@"https://maps.googleapis.com/maps/api/geocode/json?latlng={nmea.Latitude},{nmea.Longitude}&key={_apiKey}";

            string jsonGeocode = GetJsonFromUrl(_url);

            if (jsonGeocode != string.Empty)
            {
                JObject locJson = JObject.Parse(jsonGeocode);

                var road   = locJson["address"]?["road"]?.ToString();
                var town   = locJson["address"]?["town"]?.ToString();
                var county = locJson["address"]?["county"]?.ToString();
                var state  = locJson["address"]?["state"]?.ToString();

                return new GeoCodeLocation(road ?? "None", town ?? "None", county ?? "None", state ?? "None", nmea.GetCardinalDirection(nmea.Course));
            }
            else return new GeoCodeLocation("No Data", "No Data", "No Data", "No Data", "No Data");
        }

        private static string GetJsonFromUrl(string url)
        {
            if (url != null)
            {
                try
                {
                    return url
                        .WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3")
                        .GetStringAsync().Result;
                } catch (Exception ex)
                {
                    return string.Empty;
                }
            }

            else return string.Empty;
        }
    }
}

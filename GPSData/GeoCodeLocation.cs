using Newtonsoft.Json;

namespace GPSData
{
    public class GeoCodeLocation : IGeoCodeLocation
    {
        public string Road { get; set; }
        public string City { get; set; }
        public string County { get; set; }
        public string State { get; set; }
        public string Heading { get; set; }

        public GeoCodeLocation(string road, string city, string county, string state, string heading)
        {
            Road = road;
            City = city;
            County = county;
            State = state;
            Heading = heading;
        }
    }
}

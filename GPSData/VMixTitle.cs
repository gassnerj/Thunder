using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSData
{
    public class VMixTitle
    {
        public string Headline { get; set; }
        public string Description { get; set; }

        public VMixTitle(IGeoCodeLocation geoCodeLocation) 
        {
            Headline = $"{geoCodeLocation.City}, {geoCodeLocation.County}, {geoCodeLocation.State}";
            Description = $"{geoCodeLocation.Road} (Direction: {geoCodeLocation.Heading})";
        }

        public override string ToString()
        {
            string result = JsonConvert.SerializeObject(this);
            return $"[{result}]";
        }
    }
}

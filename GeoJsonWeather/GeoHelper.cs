using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GeoJsonWeather;

public static class GeoHelper
{
    private const double _EARTH_RADIUS_KM = 6371.0;



    public static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return _EARTH_RADIUS_KM * c;
    }

    private static double ToRadians(double angleInDegrees)
    {
        return angleInDegrees * Math.PI / 180.0;
    }
    
    public static bool IsPointInPolygon(double latitude, double longitude, List<Coordinate> polygon)
    {
        int i, j     = polygon.Count - 1;
        var oddNodes = false;

        for (i = 0; i < polygon.Count; i++)
        {
            if ((polygon[i].Longitude < longitude && polygon[j].Longitude >= longitude || polygon[j].Longitude < longitude && polygon[i].Longitude >= longitude)
                && (polygon[i].Latitude <= latitude || polygon[j].Latitude <= latitude))
            {
                oddNodes ^= (polygon[i].Latitude + (longitude - polygon[i].Longitude) / (polygon[j].Longitude - polygon[i].Longitude) * (polygon[j].Latitude - polygon[i].Latitude) < latitude);
            }

            j = i;
        }

        return oddNodes;
    }

}
using System;
using System.Collections.Generic;

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
}
using System;

namespace ThunderApp.Models;

/// <summary>
/// Simple geographic bounds (lat/lon degrees). Used for cheap "does this alert intersect my radius" checks.
/// We intentionally use bounds instead of centroids so large/irregular polygons don't get incorrectly filtered out.
/// </summary>
public readonly record struct GeoBounds(double MinLat, double MinLon, double MaxLat, double MaxLon)
{
    public static GeoBounds FromPoint(double lat, double lon) => new(lat, lon, lat, lon);

    public static GeoBounds Union(GeoBounds a, GeoBounds b)
        => new(
            Math.Min(a.MinLat, b.MinLat),
            Math.Min(a.MinLon, b.MinLon),
            Math.Max(a.MaxLat, b.MaxLat),
            Math.Max(a.MaxLon, b.MaxLon));

    /// <summary>
    /// Returns an approximate minimum distance (miles) from a point to this bounds.
    /// If the point is inside the bounds, distance is 0.
    /// </summary>
    public double DistanceMilesTo(GeoPoint p)
    {
        // Clamp point to the bounds rectangle.
        double clampedLat = Math.Clamp(p.Lat, MinLat, MaxLat);
        double clampedLon = Math.Clamp(p.Lon, MinLon, MaxLon);

        // If inside, clamp == point => distance 0.
        return HaversineMiles(p.Lat, p.Lon, clampedLat, clampedLon);
    }

    private static double HaversineMiles(double lat1, double lon1, double lat2, double lon2)
        => HaversineMeters(lat1, lon1, lat2, lon2) / 1609.344;

    private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        static double ToRad(double deg) => deg * (Math.PI / 180.0);

        double dLat = ToRad(lat2 - lat1);
        double dLon = ToRad(lon2 - lon1);

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}

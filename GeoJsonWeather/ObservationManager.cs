#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GeoJsonWeather.Api;
using GeoJsonWeather.Models;
using GeoJsonWeather.Parsers;

namespace GeoJsonWeather;

public class ObservationManager
{
    public static async IAsyncEnumerable<StationObservationSnapshot> GetNearestObservations(
        double latitude,
        double longitude,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        string latRaw = latitude.ToString("0.########", CultureInfo.InvariantCulture);
        string lonRaw = longitude.ToString("0.########", CultureInfo.InvariantCulture);
        var pointsUrl = $"https://api.weather.gov/points/{latRaw},{lonRaw}";

        WebData.Logger?.Invoke($"WX points url={pointsUrl}");

        ForecastPointModel? point;
        try
        {
            var pointsMgr = new ApiManager(new ApiFetcher(string.Empty, pointsUrl));
            point = await pointsMgr.GetModelAsync(new ForecastPointParser(), ct);
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("HTTP 403", StringComparison.OrdinalIgnoreCase))
        {
            string lat4 = latitude.ToString("0.####", CultureInfo.InvariantCulture);
            string lon4 = longitude.ToString("0.####", CultureInfo.InvariantCulture);
            var retryUrl = $"https://api.weather.gov/points/{lat4},{lon4}";
            WebData.Logger?.Invoke($"WX points retry url={retryUrl} after 403");

            await Task.Delay(500, ct);
            var retryMgr = new ApiManager(new ApiFetcher(string.Empty, retryUrl));
            point = await retryMgr.GetModelAsync(new ForecastPointParser(), ct);
        }
        WebData.Logger?.Invoke(point is null ? "WX points: null" : $"WX points OK zoneUrl={point.ZoneUrl}");
        if (point is null || string.IsNullOrWhiteSpace(point.ZoneUrl))
            yield break;

        var zoneMgr = new ApiManager(new ApiFetcher(string.Empty, point.ZoneUrl));
        ForecastZoneModel? zone = await zoneMgr.GetModelAsync(new ForecastZoneParser(), ct);
        WebData.Logger?.Invoke(zone is null ? "WX zone: null" : $"WX zone OK stations={zone.ObservationStationUrls?.Count ?? 0}");
        if (zone?.ObservationStationUrls is null || zone.ObservationStationUrls.Count == 0)
            yield break;

        var stations = new List<ObservationStationModel>();

        // Keep sequential for now (simple + safe). We can parallelize later with a cap.
        foreach (string stationUrl in zone.ObservationStationUrls)
        {
            ct.ThrowIfCancellationRequested();

            var stationMgr = new ApiManager(new ApiFetcher(string.Empty, stationUrl));
            ObservationStationModel? station = await stationMgr.GetModelAsync(new ObservationStationParser(), ct);
            if (station != null) WebData.Logger?.Invoke($"WX station OK id={station.StationIdentifier} name={station.Name}");
            if (station != null) stations.Add(station);
        }

        if (stations.Count == 0)
            yield break;

        // Loop while inside zone polygon
        while (!ct.IsCancellationRequested &&
               GeoHelper.IsPointInPolygon(latitude, longitude, zone.ZonePolygonCoordinates))
        {
            ObservationStationModel nearest = FindNearestStation(latitude, longitude, stations);
            var stationObs = new Dictionary<string, ObservationModel?>(StringComparer.OrdinalIgnoreCase);

            foreach (var st in stations)
            {
                var sid = st.StationIdentifier;
                if (string.IsNullOrWhiteSpace(sid))
                    continue;

                var obsUrl = $"https://api.weather.gov/stations/{sid}/observations/latest";
                ObservationModel? obs = null;
                WebData.Logger?.Invoke($"WX obs latest url={obsUrl}");
                try
                {
                    var obsMgr = new ApiManager(new ApiFetcher(string.Empty, obsUrl));
                    obs = await obsMgr.GetModelAsync(new ObservationParser(), ct);
                }
                catch
                {
                }

                stationObs[sid] = obs;
            }

            ObservationModel? activeObs = null;
            if (!string.IsNullOrWhiteSpace(nearest.StationIdentifier))
                stationObs.TryGetValue(nearest.StationIdentifier, out activeObs);

            yield return new StationObservationSnapshot
            {
                Observation = activeObs,
                ActiveStation = nearest,
                Stations = stations,
                StationObservations = stationObs
            };

            // Delay AFTER yielding so you get an immediate first update.
            await Task.Delay(60000, ct);
        }
    }

    private static ObservationStationModel FindNearestStation(
        double targetLatitude,
        double targetLongitude,
        List<ObservationStationModel> stations)
    {
        string? nearestStationId = null;
        var minDistance = double.MaxValue;

        foreach (ObservationStationModel station in stations)
        {
            double distance = GeoHelper.CalculateHaversineDistance(
                targetLatitude, targetLongitude,
                station.Coordinates.Latitude, station.Coordinates.Longitude);

            if (!(distance < minDistance)) continue;
            minDistance = distance;
            nearestStationId = station.Id;
        }

        return stations.First(x => x.Id == nearestStationId);
    }
}
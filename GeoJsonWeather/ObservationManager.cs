#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeoJsonWeather.Api;
using GeoJsonWeather.Models;
using GeoJsonWeather.Parsers;

namespace GeoJsonWeather;

public class ObservationManager
{

    public static async IAsyncEnumerable<ObservationModel?> GetNearestObservations(double latitude, double longitude)
    {
        var url = $"https://api.weather.gov/points/{latitude},{longitude}";

        var                 apiFetcher = new ApiFetcher(string.Empty, url);
        var                 apiManager = new ApiManager(apiFetcher);
        var                 apiParser  = new ForecastPointParser();
        ForecastPointModel? model      = apiManager.GetModel(apiParser);

        if (model is null)
            yield return null;

        var                zoneApiFetcher = new ApiFetcher(string.Empty, model.ZoneUrl);
        var                zoneApiManager = new ApiManager(zoneApiFetcher);
        var                zoneApiParser  = new ForecastZoneParser();
        ForecastZoneModel? zoneModel      = zoneApiManager.GetModel(zoneApiParser);

        if (zoneModel.ObservationStationUrls.Count == 0)
        {
            yield return null;
        }

        var observationStationModels = new List<ObservationStationModel>();

        foreach (string stationUrl in zoneModel.ObservationStationUrls)
        {
            var                      stationApiFetcher = new ApiFetcher(string.Empty, stationUrl);
            var                      stationApiManager = new ApiManager(stationApiFetcher);
            var                      stationApiParser  = new ObservationStationParser();
            ObservationStationModel? stationModel      = stationApiManager.GetModel(stationApiParser);
            observationStationModels.Add(stationModel);
        }

        while (GeoHelper.IsPointInPolygon(latitude, longitude, zoneModel.ZonePolygonCoordinates))
        {
            await Task.Delay(60000);
            
            ObservationStationModel nearestStation = FindNearestStation(latitude, longitude, observationStationModels);

            var nearestStationUrl = $"https://api.weather.gov/stations/{nearestStation.StationIdentifier}/observations/latest";

            var observationApiFetcher = new ApiFetcher(string.Empty, nearestStationUrl);
            var observationApiManager = new ApiManager(observationApiFetcher);
            var observationApiParser  = new ObservationParser();
            yield return observationApiManager.GetModel(observationApiParser);
        }
    }

    private static ObservationStationModel FindNearestStation(double targetLatitude, double targetLongitude, List<ObservationStationModel> stations)
    {
        string? nearestStationId = null;
        var     minDistance      = double.MaxValue;

        foreach (ObservationStationModel station in stations)
        {
            double distance = GeoHelper.CalculateHaversineDistance(targetLatitude, targetLongitude, station.Coordinates.Latitude, station.Coordinates.Longitude);

            if (!(distance < minDistance))
                continue;
            minDistance      = distance;
            nearestStationId = station.Id;
        }

        return stations.First(x => x.Id == nearestStationId);
    }
}
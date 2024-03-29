﻿using System.Collections.Generic;

namespace GeoJsonWeather.Models;

public class ForecastZoneModel
{
    private readonly string _url;

    public string Id { get; set; }
    public string Name { get; set; }
    public string State { get; set; }
    public string CWA { get; set; }
    public string ForecastOfficeUrl { get; set; }
    public string TimeZone { get; set; }
    public List<string> ObservationStationUrls { get; set; }
    public List<Coordinate> ZonePolygonCoordinates { get; set; }

    public ForecastZoneModel()
    {
        ObservationStationUrls = new List<string>();
        ZonePolygonCoordinates = new List<Coordinate>();
    }

    public ForecastZoneModel(string url)
    {
        _url                   = url;
        ObservationStationUrls = new List<string>();
    }
}
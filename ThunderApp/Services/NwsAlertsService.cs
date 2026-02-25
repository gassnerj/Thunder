using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeoJsonWeather;
using ThunderApp.Models;

namespace ThunderApp.Services;

public sealed class NwsAlertsService : INwsAlertsService
{
    private readonly FeatureCollection _fc = new();
    private readonly NwsApiStringBuilder _url = new();

    public NwsAlertsService(string userAgent)
    {
        // If your GeoJsonWeather library supports setting UserAgent, do it there.
        // Otherwise leave it.
    }

    public async Task<IReadOnlyList<NwsAlert>> GetActiveAlertsAsync(CancellationToken ct)
    {
        await _fc.FetchData(_url.GetAll());
        return Map(_fc.Alerts);
    }

    public async Task<IReadOnlyList<NwsAlert>> GetActiveAlertsForPointAsync(GeoPoint point, CancellationToken ct)
    {
        await _fc.FetchData(_url.GetByLatLon(point.Lat, point.Lon));
        return Map(_fc.Alerts);
    }

    public async Task<IReadOnlyList<NwsAlert>> GetActiveAlertsByEventsAsync(IEnumerable<string> eventNames, CancellationToken ct)
    {
        // NWS supports multiple event filters by repeating the query parameter.
        // Example: /alerts/active?event=Tornado%20Warning&event=Severe%20Thunderstorm%20Warning
        var ev = (eventNames ?? Enumerable.Empty<string>())
            .Select(s => (s ?? "").Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (ev.Count == 0)
            return Array.Empty<NwsAlert>();

        var qs = string.Join("&", ev.Select(e => "event=" + System.Uri.EscapeDataString(e)));
        var url = $"https://api.weather.gov/alerts/active?{qs}";

        await _fc.FetchData(url);
        return Map(_fc.Alerts);
    }

    private static IReadOnlyList<NwsAlert> Map(IEnumerable<IAlert> src)
    {
        return src.Select(a => new NwsAlert
        {
            Event = a.Event,
            Severity = a.Severity,
            Effective = a.Effective,
            Expires = a.Expires,
            Ends = a.Ends,
            Onset = a.Onset,
            Headline = a.Headline,
            SenderName = a.SenderName,
            Description = a.Description,
            Instruction = a.Instruction,
            AreaDescription = a.AreaDescription,
            Id = a.ID,
            GeometryJson = a.GeometryJson,
            AffectedZonesUrls =  a.AffectedZonesUrls
        }).ToList();
    }
}
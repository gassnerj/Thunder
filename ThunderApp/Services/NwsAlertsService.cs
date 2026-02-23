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
            Id = a.ID
        }).ToList();
    }
}
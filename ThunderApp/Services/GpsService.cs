using System.Threading;
using System.Threading.Tasks;
using ThunderApp.Models;

namespace ThunderApp.Services;

public sealed class GpsService : IGpsService
{
    private GeoPoint? _current;

    public void SetCurrent(double lat, double lon)
    {
        _current = new GeoPoint(lat, lon);
    }

    public bool IsAvailable { get; }

    public Task<GeoPoint?> GetCurrentAsync(CancellationToken ct)
    {
        return Task.FromResult(_current);
    }
}
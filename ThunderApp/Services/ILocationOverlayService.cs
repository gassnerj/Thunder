using System.Threading;
using System.Threading.Tasks;
using ThunderApp.Models;

namespace ThunderApp.Services;

public interface ILocationOverlayService
{
    Task<LocationOverlaySnapshot> GetSnapshotAsync(GeoPoint gps, CancellationToken ct);
}

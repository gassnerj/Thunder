using System.Threading;
using System.Threading.Tasks;
using ThunderApp.Models;

namespace ThunderApp.Services;

public interface IGpsService
{
    bool IsAvailable { get; }
    Task<GeoPoint?> GetCurrentAsync(CancellationToken ct);
}
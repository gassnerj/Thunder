using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThunderApp.Models;

namespace ThunderApp.Services;

public interface INwsAlertsService
{
    Task<IReadOnlyList<NwsAlert>> GetActiveAlertsAsync(CancellationToken ct);
    Task<IReadOnlyList<NwsAlert>> GetActiveAlertsForPointAsync(GeoPoint point, CancellationToken ct);
}

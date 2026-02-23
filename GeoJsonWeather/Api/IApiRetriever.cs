using System.Threading;
using System.Threading.Tasks;

namespace GeoJsonWeather.Api;

public interface IApiRetriever
{
    Task<string> GetData(CancellationToken ct);
}
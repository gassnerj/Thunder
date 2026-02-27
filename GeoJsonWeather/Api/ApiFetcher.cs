using System.Threading;
using System.Threading.Tasks;

namespace GeoJsonWeather.Api;

public class ApiFetcher : ApiRetrieverBase
{
    // Keep a non-empty, NWS-compliant default UA.
    private readonly string _userAgent = NwsDefaults.UserAgent;
    private readonly string _url;

    public ApiFetcher(string userAgent, string url) : base(url, userAgent)
    {
        _url = url;
        if (!string.IsNullOrWhiteSpace(userAgent))
        {
            _userAgent = userAgent;
        }
    }

    // ✅ This makes your screenshot snippet compile
    public Task<string> FetchData(CancellationToken ct = default)
        => ((IApiRetriever)this).GetData(ct);
}
using System.Threading;
using System.Threading.Tasks;

namespace GeoJsonWeather.Api;

public abstract class ApiRetrieverBase : IApiRetriever
{
    private readonly string _url;
    // api.weather.gov requires a descriptive User-Agent including contact info.
    private readonly string _userAgent = NwsDefaults.UserAgent;

    protected ApiRetrieverBase(string url, string userAgent)
    {
        _url = url;
        if (!string.IsNullOrWhiteSpace(userAgent))
            _userAgent = userAgent;
    }

    async Task<string> IApiRetriever.GetData(CancellationToken ct)
    {
        // Honor cancellation + avoid hanging calls.
        return await WebData.SendHttpRequestAsync(_userAgent, _url, ct).ConfigureAwait(false);
    }
}

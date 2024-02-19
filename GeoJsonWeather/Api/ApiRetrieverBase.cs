using System.Threading.Tasks;

namespace GeoJsonWeather.Api;

public abstract class ApiRetrieverBase : IApiRetriever
{
    private readonly string _url;
    private readonly string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:122.0) Gecko/20100101 Firefox/122.0";

    protected ApiRetrieverBase(string url, string userAgent)
    {
        _url = url;
        if (!string.IsNullOrEmpty(userAgent))
            _userAgent = userAgent;
    }

    async Task<string> IApiRetriever.GetData()
    {
        return await WebData.SendHttpRequestAsync(_userAgent, _url);
    }
}
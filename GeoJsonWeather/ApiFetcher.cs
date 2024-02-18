using System.Threading.Tasks;

namespace GeoJsonWeather;

public class ApiFetcher
{
    private readonly IWebData _webData;
    private readonly string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:97.0) Gecko/20100101 Firefox/97.0";
    
    public ApiFetcher(IWebData webData, string userAgent)
    {
        _webData = webData;
        if (!string.IsNullOrEmpty(userAgent))
        {
            _userAgent = userAgent;
        }
    }

    public async Task<string> FetchData()
    {
        return await _webData.SendHttpRequestAsync(_userAgent);
    }
}
using System.Threading.Tasks;
using GeoJsonWeather.Models;

namespace GeoJsonWeather.Api;

public class ApiFetcher : ApiRetrieverBase
{
    private readonly string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:97.0) Gecko/20100101 Firefox/97.0";
    private readonly string _url;
    
    public ApiFetcher(string userAgent, string url) : base(url, userAgent)
    {
        _url     = url;
        if (!string.IsNullOrEmpty(userAgent))
        {
            _userAgent = userAgent;
        }
    }
}
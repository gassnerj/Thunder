using System.Threading.Tasks;

namespace GeoJsonWeather.Api;

public interface IWebData
{
    Task<string> SendHttpRequestAsync(string url);
    Task<string> SendHttpRequestAsync(string userAgent, string url);
}
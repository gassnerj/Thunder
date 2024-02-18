using System.Threading.Tasks;

namespace GeoJsonWeather.Api;

public interface IWebData
{
    Task<string> SendHttpRequestAsync();
    Task<string> SendHttpRequestAsync(string userAgent);
}
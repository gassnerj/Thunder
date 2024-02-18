using System.Threading.Tasks;

namespace GeoJsonWeather;

public interface IWebData
{
    Task<string> SendHttpRequestAsync();
    Task<string> SendHttpRequestAsync(string userAgent);
}
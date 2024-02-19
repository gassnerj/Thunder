using System.Threading.Tasks;

namespace GeoJsonWeather.Api;

public interface IApiRetriever
{
    Task<string> GetData();
}
using System.Threading.Tasks;

namespace GeoJsonWeather.Models;

public interface IApiRetriever
{
    Task<string> GetData();
}
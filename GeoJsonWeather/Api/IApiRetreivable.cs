#nullable enable
using System.Threading.Tasks;

namespace GeoJsonWeather.Stations;

public interface IApiRetreivable
{
    Task<string> GetData();
}
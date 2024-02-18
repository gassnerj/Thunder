using System.Threading.Tasks;

namespace GeoJsonWeather
{
    public interface IDataReadOnlyAccess
    {
        Task<T> GetDataAsync<T>() where T : class;
    }
}
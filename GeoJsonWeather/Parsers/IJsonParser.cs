using System.Text.Json;

namespace GeoJsonWeather.Parsers;

public interface IJsonParser<T>
{
    T GetItem(JsonElement jsonElement);
}
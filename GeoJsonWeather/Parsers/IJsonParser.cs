namespace GeoJsonWeather.Parsers;

public interface IJsonParser<T>
{
    T GetItem();
}
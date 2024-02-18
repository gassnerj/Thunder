#nullable enable
using System.Collections.Generic;

namespace GeoJsonWeather.Parsers;

public interface IJsonParsers
{
    IEnumerable<T> GetItems<T>();
}
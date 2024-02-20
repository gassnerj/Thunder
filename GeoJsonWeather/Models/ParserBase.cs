using System;
using GeoJsonWeather.Parsers;

namespace GeoJsonWeather.Models;

public class ParserBase
{
    internal protected static DateTime ISO8601Parse(string dateTime)
    {
        bool success = DateTime.TryParse(dateTime, out DateTime date);
        return success ? date : DateTime.Now;
    }
}
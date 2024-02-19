using System;
using GeoJsonWeather.Parsers;

namespace GeoJsonWeather.Api;

public class ApiManager
{
    private readonly IApiRetriever _apiRetriever;

    public ApiManager(IApiRetriever apiRetriever)
    {
        _apiRetriever = apiRetriever ?? throw new ArgumentNullException(nameof(apiRetriever));
    }

    public T GetModel<T>(IJsonParser<T> jsonParser)
    {
        string json = _apiRetriever.GetData().Result;
        return jsonParser.GetItem(json);
    }
}
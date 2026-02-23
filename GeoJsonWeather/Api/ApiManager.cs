using System;
using System.Threading;
using System.Threading.Tasks;
using GeoJsonWeather.Parsers;

namespace GeoJsonWeather.Api;

public class ApiManager
{
    private readonly IApiRetriever _apiRetriever;

    public ApiManager(IApiRetriever apiRetriever)
    {
        _apiRetriever = apiRetriever ?? throw new ArgumentNullException(nameof(apiRetriever));
    }

    public async Task<T?> GetModelAsync<T>(IJsonParser<T> jsonParser, CancellationToken ct = default)
    {
        string json = await _apiRetriever.GetData(ct).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(json) ? default : jsonParser.GetItem(json);
    }

    // Keep this ONLY for legacy callers (ideally delete later)
    public T? GetModel<T>(IJsonParser<T> jsonParser)
        => GetModelAsync(jsonParser).GetAwaiter().GetResult();
}
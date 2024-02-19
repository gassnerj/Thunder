using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeoJsonWeather.Api;
using GeoJsonWeather.Parsers;

namespace GeoJsonWeather.Models;

public class ForecastZoneModel
{
    private readonly string _url;

    public string Id { get; set; }
    public string Name { get; set; }
    public string State { get; set; }
    public string CWA { get; set; }
    public string ForecastOfficeUrl { get; set; }
    public string TimeZone { get; set; }
    public List<string> ObservationStationUrls { get; set; }

    public ForecastZoneModel()
    {
        ObservationStationUrls = new List<string>();
    }

    public ForecastZoneModel(string url)
    {
        _url                   = url;
        ObservationStationUrls = new List<string>();
    }
}

public abstract class ApiRetrieverBase : IApiRetriever
{
    private readonly string _url;
    private readonly string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:97.0) Gecko/20100101 Firefox/97.0";

    protected ApiRetrieverBase(string url, string userAgent)
    {
        _url        = url;
        if (!string.IsNullOrEmpty(userAgent))
            _userAgent = userAgent;
    }

    async Task<string> IApiRetriever.GetData()
    {
        return await WebData.SendHttpRequestAsync(_userAgent, _url);
    }
}

public interface IApiRetriever
{
    Task<string> GetData();
}

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

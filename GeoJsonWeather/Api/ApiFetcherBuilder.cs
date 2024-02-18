namespace GeoJsonWeather.Api;

public class ApiFetcherBuilder
{
    private readonly string _url;
    private const string _USER_AGENT = "GeoJsonWeather/1.0 (Windows; U; Windows NT 5.1; en-US; rv:1.8.1.6) Gecko/20070725 Firefox/2.0.0.6";

    public ApiFetcherBuilder(string url)
    {
        _url       = url;
    }
    
    public ApiFetcher Build()
    {
        var webData = new WebData(_url);
        return new ApiFetcher(webData, _USER_AGENT);
    }
}

namespace GeoJsonWeather.Api;

public sealed class ApiFetcherBuilder
{
    private readonly string _url;
    private string _userAgent = "";

    public ApiFetcherBuilder(string url)
    {
        _url = url;
    }

    public ApiFetcherBuilder WithUserAgent(string userAgent)
    {
        _userAgent = userAgent ?? "";
        return this;
    }

    public ApiFetcher Build()
        => new ApiFetcher(_userAgent, _url);
}
namespace GeoJsonWeather.Api;

public sealed class ApiFetcherBuilder
{
    private readonly string _url;
    // api.weather.gov requires a descriptive User-Agent including contact info.
    // Default to our app UA to avoid accidental blank/invalid requests.
    private string _userAgent = NwsDefaults.UserAgent;

    public ApiFetcherBuilder(string url)
    {
        _url = url;
    }

    public ApiFetcherBuilder WithUserAgent(string userAgent)
    {
        _userAgent = string.IsNullOrWhiteSpace(userAgent) ? NwsDefaults.UserAgent : userAgent;
        return this;
    }

    public ApiFetcher Build()
        => new ApiFetcher(_userAgent, _url);
}
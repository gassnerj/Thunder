using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ThunderApp.Models;

namespace ThunderApp.Services;

public sealed class ReverseGeocodeCoordinator : ILocationOverlayService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(6);
    private static readonly TimeSpan MinSuccessInterval = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(500);
    private const double MinMoveMiles = 0.25;

    private readonly HttpClient _http;
    private readonly string _mapboxToken;
    private readonly string _nominatimBaseUrl;
    private readonly string _nominatimUserAgent;
    private readonly Action<string>? _log;

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private GeoPoint? _lastGeocodePoint;
    private DateTime _lastSuccessUtc = DateTime.MinValue;
    private DateTime _lastFailureLogUtc = DateTime.MinValue;
    private string _lastProvider = "";
    private DateTime _mapboxRateLimitUntilUtc = DateTime.MinValue;

    private GeocodeResult? _lastGood;
    private GeoPoint? _latestPendingPoint;
    private CancellationTokenSource? _debounceCts;
    private Task? _debounceTask;

    public ReverseGeocodeCoordinator(HttpClient http, string mapboxToken, string nominatimBaseUrl, string nominatimUserAgent, Action<string>? log)
    {
        _http = http;
        _mapboxToken = mapboxToken ?? "";
        _nominatimBaseUrl = string.IsNullOrWhiteSpace(nominatimBaseUrl) ? "https://nominatim.openstreetmap.org" : nominatimBaseUrl.TrimEnd('/');
        _nominatimUserAgent = string.IsNullOrWhiteSpace(nominatimUserAgent)
            ? "ThunderApp/1.0 (contact: thunderapp@users.noreply.github.com)"
            : nominatimUserAgent;
        _log = log;
    }

    public async Task<LocationOverlaySnapshot> GetSnapshotAsync(GeoPoint gps, CancellationToken ct)
    {
        await TryScheduleRefreshAsync(gps, ct);

        var best = _lastGood;
        if (best is null)
        {
            return LocationOverlayFormatting.ComposeSnapshot(gps, null, null, "GPS");
        }

        return LocationOverlayFormatting.ComposeSnapshot(gps, best, _lastGood?.PlacePoint, best.Source);
    }

    private async Task TryScheduleRefreshAsync(GeoPoint gps, CancellationToken ct)
    {
        bool shouldRefresh = _lastGeocodePoint is null
                             || DistanceMiles(_lastGeocodePoint.Value, gps) >= MinMoveMiles
                             || DateTime.UtcNow - _lastSuccessUtc >= MinSuccessInterval;

        if (!shouldRefresh) return;

        _latestPendingPoint = gps;

        // Coalesce bursts without starving under continuous high-frequency GPS updates.
        if (_debounceTask is { IsCompleted: false })
            return;

        _debounceCts?.Cancel();
        _debounceCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var token = _debounceCts.Token;

        _debounceTask = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(Debounce, token);
                var p = _latestPendingPoint;
                if (p is null) return;
                await RefreshNowAsync(p.Value, token);
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    private async Task RefreshNowAsync(GeoPoint gps, CancellationToken ct)
    {
        string key = LocationOverlayFormatting.CacheKey(gps.Lat, gps.Lon);
        if (_cache.TryGetValue(key, out var cached) && DateTime.UtcNow - cached.StoredUtc < CacheTtl)
        {
            _lastGood = cached.Result;
            _lastGeocodePoint = gps;
            _lastSuccessUtc = DateTime.UtcNow;
            return;
        }

        await _gate.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue(key, out cached) && DateTime.UtcNow - cached.StoredUtc < CacheTtl)
            {
                _lastGood = cached.Result;
                _lastGeocodePoint = gps;
                _lastSuccessUtc = DateTime.UtcNow;
                return;
            }

            GeocodeResult? result = await TryMapboxAsync(gps, ct) ?? await TryNominatimAsync(gps, ct);
            if (result is null)
            {
                if (DateTime.UtcNow - _lastFailureLogUtc > TimeSpan.FromMinutes(1))
                {
                    _lastFailureLogUtc = DateTime.UtcNow;
                    _log?.Invoke("Location geocode failed (using last-good data)");
                }
                return;
            }

            if (!string.Equals(result.Source, _lastProvider, StringComparison.OrdinalIgnoreCase))
            {
                _log?.Invoke($"Location geocode provider: {result.Source}");
                _lastProvider = result.Source;
            }

            _lastGood = result;
            _lastGeocodePoint = gps;
            _lastSuccessUtc = DateTime.UtcNow;
            _cache[key] = new CacheEntry(DateTime.UtcNow, result);
            _log?.Invoke($"Location geocode refresh ok source={result.Source} ms={result.ElapsedMs}");
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<GeocodeResult?> TryMapboxAsync(GeoPoint gps, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_mapboxToken)) return null;
        if (DateTime.UtcNow < _mapboxRateLimitUntilUtc) return null;

        var sw = Stopwatch.StartNew();
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(2.8));

            string lat = gps.Lat.ToString("0.######", CultureInfo.InvariantCulture);
            string lon = gps.Lon.ToString("0.######", CultureInfo.InvariantCulture);
            string v6Url = $"https://api.mapbox.com/search/geocode/v6/reverse?longitude={lon}&latitude={lat}&types=address,street,place,region,neighborhood,locality&limit=8&access_token={Uri.EscapeDataString(_mapboxToken)}";

            using var reqV6 = new HttpRequestMessage(HttpMethod.Get, v6Url);
            using var resV6 = await _http.SendAsync(reqV6, timeout.Token);

            string? payload = null;
            if (resV6.IsSuccessStatusCode)
            {
                payload = await resV6.Content.ReadAsStringAsync(timeout.Token);
            }
            else if ((int)resV6.StatusCode is 401 or 403)
            {
                _log?.Invoke($"Mapbox geocode auth error ({(int)resV6.StatusCode}); using fallback provider.");
                return null;
            }
            else if ((int)resV6.StatusCode == 429)
            {
                _mapboxRateLimitUntilUtc = DateTime.UtcNow.AddMinutes(20);
                _log?.Invoke("Mapbox geocode rate-limited; temporarily using fallback provider.");
                return null;
            }
            else
            {
                // Fallback to the legacy endpoint in case v6 is blocked for this token.
                string v5Url = $"https://api.mapbox.com/geocoding/v5/mapbox.places/{lon},{lat}.json?types=address,poi,place,region&limit=6&access_token={Uri.EscapeDataString(_mapboxToken)}";
                using var reqV5 = new HttpRequestMessage(HttpMethod.Get, v5Url);
                using var resV5 = await _http.SendAsync(reqV5, timeout.Token);
                if (!resV5.IsSuccessStatusCode) return null;
                payload = await resV5.Content.ReadAsStringAsync(timeout.Token);
            }

            if (string.IsNullOrWhiteSpace(payload)) return null;

            using var doc = JsonDocument.Parse(payload);
            if (!doc.RootElement.TryGetProperty("features", out var features) || features.ValueKind != JsonValueKind.Array || features.GetArrayLength() == 0)
                return null;

            string road = "";
            string city = "";
            string state = "";
            double? placeLat = null;
            double? placeLon = null;

            foreach (var f in features.EnumerateArray())
            {
                var placeType = ReadFeatureType(f);
                string text = ReadFirst(f, "text", "name", "feature_name", "place_name");
                if (string.IsNullOrWhiteSpace(text))
                    text = ReadPropertyFirst(f, "name", "place_formatted", "full_address");

                if (string.IsNullOrWhiteSpace(road) && (placeType is "address" or "poi"))
                    road = text;

                if (string.IsNullOrWhiteSpace(road) && placeType is "street" or "neighborhood")
                    road = text;

                if (placeType == "place" && string.IsNullOrWhiteSpace(city))
                {
                    city = text;
                    if (f.TryGetProperty("center", out var c) && c.ValueKind == JsonValueKind.Array && c.GetArrayLength() == 2)
                    {
                        placeLon = c[0].GetDouble();
                        placeLat = c[1].GetDouble();
                    }
                    else if (f.TryGetProperty("geometry", out var g)
                        && g.TryGetProperty("coordinates", out var gc)
                        && gc.ValueKind == JsonValueKind.Array
                        && gc.GetArrayLength() == 2)
                    {
                        placeLon = gc[0].GetDouble();
                        placeLat = gc[1].GetDouble();
                    }
                }

                if (f.TryGetProperty("context", out var ctxArr) && ctxArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var c in ctxArr.EnumerateArray())
                    {
                        string id = c.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
                        string tx = ReadFirst(c, "text", "name");
                        string shortCode = c.TryGetProperty("short_code", out var sc) ? sc.GetString() ?? "" : "";

                        if (string.IsNullOrWhiteSpace(city) && id.StartsWith("place.")) city = tx;
                        if (string.IsNullOrWhiteSpace(state) && id.StartsWith("region.")) state = AbbrevState(!string.IsNullOrWhiteSpace(shortCode) ? shortCode : tx);
                    }
                }

                if (f.TryGetProperty("properties", out var props) && props.ValueKind == JsonValueKind.Object)
                {
                    if (string.IsNullOrWhiteSpace(city)
                        && props.TryGetProperty("context", out var v6Ctx)
                        && v6Ctx.ValueKind == JsonValueKind.Object)
                    {
                        if (v6Ctx.TryGetProperty("place", out var placeObj) && placeObj.ValueKind == JsonValueKind.Object)
                            city = ReadFirst(placeObj, "name", "text");
                        if (string.IsNullOrWhiteSpace(city)
                            && v6Ctx.TryGetProperty("locality", out var locObj)
                            && locObj.ValueKind == JsonValueKind.Object)
                            city = ReadFirst(locObj, "name", "text");
                    }

                    if (string.IsNullOrWhiteSpace(state)
                        && props.TryGetProperty("context", out var v6Ctx2)
                        && v6Ctx2.ValueKind == JsonValueKind.Object
                        && v6Ctx2.TryGetProperty("region", out var regionObj)
                        && regionObj.ValueKind == JsonValueKind.Object)
                    {
                        string regionCode = ReadFirst(regionObj, "region_code", "short_code");
                        string regionName = ReadFirst(regionObj, "name", "text");
                        state = AbbrevState(!string.IsNullOrWhiteSpace(regionCode) ? regionCode : regionName);
                    }
                }
            }

            road = LocationOverlayFormatting.NormalizeRoadName(road);
            if (string.IsNullOrWhiteSpace(city))
            {
                city = features.EnumerateArray()
                    .Select(f => ReadFirst(f, "place", "locality", "district"))
                    .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "";
            }
            sw.Stop();
            return new GeocodeResult(road, city, state, placeLat, placeLon, "Mapbox", (int)sw.ElapsedMilliseconds);
        }
        catch { return null; }
    }

    private static string ReadFirst(JsonElement obj, params string[] names)
    {
        foreach (var n in names)
        {
            if (obj.TryGetProperty(n, out var val) && val.ValueKind == JsonValueKind.String)
            {
                var s = val.GetString();
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }
        }
        return "";
    }

    private static string ReadPropertyFirst(JsonElement feature, params string[] names)
    {
        if (!feature.TryGetProperty("properties", out var props) || props.ValueKind != JsonValueKind.Object)
            return "";
        return ReadFirst(props, names);
    }

    private static string ReadFeatureType(JsonElement feature)
    {
        if (feature.TryGetProperty("place_type", out var placeTypeArr)
            && placeTypeArr.ValueKind == JsonValueKind.Array
            && placeTypeArr.GetArrayLength() > 0)
        {
            return placeTypeArr[0].GetString() ?? "";
        }

        if (feature.TryGetProperty("properties", out var props) && props.ValueKind == JsonValueKind.Object)
        {
            string type = ReadFirst(props, "feature_type", "type");
            if (!string.IsNullOrWhiteSpace(type)) return type;
        }

        return "";
    }

    private async Task<GeocodeResult?> TryNominatimAsync(GeoPoint gps, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(2.8));

            string lat = gps.Lat.ToString("0.######", CultureInfo.InvariantCulture);
            string lon = gps.Lon.ToString("0.######", CultureInfo.InvariantCulture);
            string url = $"{_nominatimBaseUrl}/reverse?format=jsonv2&lat={lat}&lon={lon}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd(_nominatimUserAgent);
            req.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");

            using var res = await _http.SendAsync(req, timeout.Token);
            if (!res.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(timeout.Token));
            if (!doc.RootElement.TryGetProperty("address", out var addr) || addr.ValueKind != JsonValueKind.Object)
                return null;

            string road = Read(addr, "highway") ?? Read(addr, "road") ?? Read(addr, "route") ?? Read(addr, "pedestrian") ?? Read(addr, "footway") ?? "";
            string city = Read(addr, "city") ?? Read(addr, "town") ?? Read(addr, "village") ?? Read(addr, "municipality") ?? Read(addr, "county") ?? "";
            string state = AbbrevState(Read(addr, "state") ?? "");

            double? placeLat = doc.RootElement.TryGetProperty("lat", out var l1) && double.TryParse(l1.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var pLat) ? pLat : null;
            double? placeLon = doc.RootElement.TryGetProperty("lon", out var l2) && double.TryParse(l2.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var pLon) ? pLon : null;

            road = LocationOverlayFormatting.NormalizeRoadName(road);
            sw.Stop();
            return new GeocodeResult(road, city, state, placeLat, placeLon, "Nominatim", (int)sw.ElapsedMilliseconds);
        }
        catch { return null; }
    }

    private static string? Read(JsonElement obj, string name)
        => obj.TryGetProperty(name, out var x) ? x.GetString() : null;

    private static string AbbrevState(string state)
    {
        if (string.IsNullOrWhiteSpace(state)) return "";
        state = state.Trim();
        if (state.StartsWith("us-", StringComparison.OrdinalIgnoreCase) && state.Length == 5)
            state = state.Substring(3);
        return state.Length == 2 ? state.ToUpperInvariant() : state;
    }

    private static double DistanceMiles(GeoPoint a, GeoPoint b)
        => GeoJsonWeather.GeoHelper.CalculateHaversineDistance(a.Lat, a.Lon, b.Lat, b.Lon) * 0.621371;

    private sealed record CacheEntry(DateTime StoredUtc, GeocodeResult Result);

    public sealed record GeocodeResult(string Road, string City, string State, double? PlaceLat, double? PlaceLon, string Source, int ElapsedMs)
    {
        public GeoPoint? PlacePoint => (PlaceLat.HasValue && PlaceLon.HasValue) ? new GeoPoint(PlaceLat.Value, PlaceLon.Value) : null;
    }
}

public static class LocationOverlayFormatting
{
    public static string CacheKey(double lat, double lon)
        => $"{Math.Round(lat, 4):0.0000}:{Math.Round(lon, 4):0.0000}";

    public static LocationOverlaySnapshot ComposeSnapshot(GeoPoint gps, ReverseGeocodeCoordinator.GeocodeResult? geo, GeoPoint? placePoint, string source)
    {
        string road = geo?.Road ?? "";
        string city = geo?.City ?? "";
        string state = geo?.State ?? "";

        double dist = 0.1;
        string dir = "N";

        if (placePoint is GeoPoint pp)
        {
            dist = Math.Max(0.1, GeoJsonWeather.GeoHelper.CalculateHaversineDistance(gps.Lat, gps.Lon, pp.Lat, pp.Lon) * 0.621371);
            dir = BearingToCompass(BearingDegrees(pp, gps));
        }

        string locLine;
        if (!string.IsNullOrWhiteSpace(road) && !string.IsNullOrWhiteSpace(city) && !string.IsNullOrWhiteSpace(state))
            locLine = $"{road} near {city}, {state}";
        else if (!string.IsNullOrWhiteSpace(city) && !string.IsNullOrWhiteSpace(state))
            locLine = $"Near {city}, {state} ({Math.Max(0.1, dist):0.0} mi {dir})";
        else
            locLine = $"{gps.Lat:0.0000}, {gps.Lon:0.0000}";

        string locDetail = $"{Math.Max(0.1, dist):0.0} mi {dir} • {gps.Lat:0.0000}, {gps.Lon:0.0000}";

        return new LocationOverlaySnapshot
        {
            GeneratedAtUtc = DateTime.UtcNow,
            LocLine = locLine,
            LocDetail = locDetail,
            Road = road,
            City = city,
            State = state,
            DistMi = Math.Max(0.1, dist),
            Dir = dir,
            Lat = gps.Lat,
            Lon = gps.Lon,
            Source = string.IsNullOrWhiteSpace(source) ? "GPS" : source
        };
    }

    public static string NormalizeRoadName(string road)
    {
        if (string.IsNullOrWhiteSpace(road)) return "";
        string s = road.Trim();

        s = Regex.Replace(s, @"\bUnited States Highway\s+(\d+)\b", "US-$1", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\bU\.?S\.?\s*Highway\s+(\d+)\b", "US-$1", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\bInterstate\s+(\d+)\b", "I-$1", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\bState Highway\s+(\d+)\b", "SH-$1", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\bFarm to Market Road\s+(\d+)\b", "FM $1", RegexOptions.IgnoreCase);

        return s;
    }

    public static string BearingToCompass(double bearing)
    {
        string[] dirs = ["N", "NE", "E", "SE", "S", "SW", "W", "NW"];
        int idx = (int)Math.Round(((bearing % 360) / 45), MidpointRounding.AwayFromZero) % 8;
        return dirs[idx];
    }

    public static double BearingDegrees(GeoPoint from, GeoPoint to)
    {
        double lat1 = DegreesToRadians(from.Lat);
        double lat2 = DegreesToRadians(to.Lat);
        double dLon = DegreesToRadians(to.Lon - from.Lon);

        double y = Math.Sin(dLon) * Math.Cos(lat2);
        double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
        var brng = Math.Atan2(y, x);
        return (RadiansToDegrees(brng) + 360) % 360;
    }

    private static double DegreesToRadians(double deg) => deg * Math.PI / 180.0;
    private static double RadiansToDegrees(double rad) => rad * 180.0 / Math.PI;
}

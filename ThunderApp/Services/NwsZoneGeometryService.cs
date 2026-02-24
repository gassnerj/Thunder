using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ThunderApp.Services;

public sealed class NwsZoneGeometryService(HttpClient http)
{
    private readonly ConcurrentDictionary<string, string?> _cache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<string?> GetGeometryJsonAsync(string zoneUrl)
    {
        if (_cache.TryGetValue(zoneUrl, out string? cached))
            return cached;

        try
        {
            string json = await http.GetStringAsync(zoneUrl);
            using JsonDocument doc = JsonDocument.Parse(json);

            JsonElement root = doc.RootElement;
            string? type = root.TryGetProperty("type", out JsonElement t) ? t.GetString() : null;

            if (string.Equals(type, "Feature", StringComparison.OrdinalIgnoreCase))
            {
                if (root.TryGetProperty("geometry", out JsonElement geom) &&
                    geom.ValueKind != JsonValueKind.Null &&
                    geom.ValueKind != JsonValueKind.Undefined)
                {
                    string raw = geom.GetRawText();
                    _cache[zoneUrl] = raw;
                    return raw;
                }
            }
            else if (string.Equals(type, "FeatureCollection", StringComparison.OrdinalIgnoreCase))
            {
                if (root.TryGetProperty("features", out JsonElement feats) && feats.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement f in feats.EnumerateArray())
                    {
                        if (!f.TryGetProperty("geometry", out JsonElement geom) ||
                            geom.ValueKind == JsonValueKind.Null ||
                            geom.ValueKind == JsonValueKind.Undefined) continue;
                        string raw = geom.GetRawText();
                        _cache[zoneUrl] = raw;
                        return raw;
                    }
                }
            }
        }
        catch
        {
            // ignored
        }

        _cache[zoneUrl] = null;
        return null;
    }
}
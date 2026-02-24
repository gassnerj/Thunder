using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ThunderApp.Services;

public sealed class NwsZoneGeometryService(HttpClient http, SimpleDiskCache disk)
{
    private readonly ConcurrentDictionary<string, string> _mem = new(StringComparer.OrdinalIgnoreCase);

    // Zone boundaries rarely change. Pick something long.
    private static readonly TimeSpan DiskTtl = TimeSpan.FromDays(30);

    public async Task<string?> GetGeometryJsonAsync(string zoneUrl)
    {
        // 1) memory
        if (_mem.TryGetValue(zoneUrl, out string? mem))
            return mem;

        // 2) disk
        string? diskHit = await disk.TryReadAsync(zoneUrl, DiskTtl).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(diskHit))
        {
            _mem[zoneUrl] = diskHit;
            return diskHit;
        }

        // 3) network
        try
        {
            string json = await http.GetStringAsync(zoneUrl).ConfigureAwait(false);
            using JsonDocument doc = JsonDocument.Parse(json);

            JsonElement root = doc.RootElement;
            string? type = root.TryGetProperty("type", out JsonElement t) ? t.GetString() : null;

            string? raw = null;

            if (string.Equals(type, "Feature", StringComparison.OrdinalIgnoreCase))
            {
                if (root.TryGetProperty("geometry", out JsonElement geom) &&
                    geom.ValueKind != JsonValueKind.Null &&
                    geom.ValueKind != JsonValueKind.Undefined)
                {
                    raw = geom.GetRawText();
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
                        raw = geom.GetRawText();
                        break;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(raw))
            {
                _mem[zoneUrl] = raw;
                await disk.WriteAsync(zoneUrl, raw).ConfigureAwait(false);
                return raw;
            }
        }
        catch
        {
            // ignore
        }

        // IMPORTANT: do NOT cache null (lets retries happen)
        return null;
    }
}
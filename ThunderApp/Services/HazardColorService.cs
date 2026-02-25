using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ThunderApp.Services;

/// <summary>
/// Loads the official NWS hazards map color palette from weather.gov/help-map
/// and caches it locally.
/// </summary>
public sealed class HazardColorService
{
    private static readonly Uri HelpMapUri = new("https://www.weather.gov/help-map");

    // Matches lines like:
    // "Tornado Warning 2   Red 255 0 0 FF0000"
    private static readonly Regex LineRx = new(
        @"^(?<name>.+?)\s+\d+\s+\S+\s+\d+\s+\d+\s+\d+\s+(?<hex>[0-9A-Fa-f]{6})\s*$",
        RegexOptions.Compiled);

    private readonly HttpClient _http;
    private readonly string _cachePath;

    public HazardColorService(HttpClient http, string cachePath)
    {
        _http = http;
        _cachePath = cachePath;
    }

    public async Task<Dictionary<string, string>> LoadOfficialAsync(CancellationToken ct)
    {
        var cached = TryReadCache();
        if (cached is not null && cached.Count > 0)
            return cached;

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, HelpMapUri);
            req.Headers.UserAgent.ParseAdd("ThunderApp/1.0 (local)");

            using var resp = await _http.SendAsync(
                req,
                HttpCompletionOption.ResponseContentRead,
                ct).ConfigureAwait(false);

            resp.EnsureSuccessStatusCode();

            var html = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var parsed = ParseFromHelpMapHtml(html);

            if (parsed.Count > 0)
                WriteCache(parsed);

            if (parsed.Count == 0)
                throw new InvalidOperationException("Parsed hazard palette was empty.");

            return parsed;
        }
        catch
        {
            // Minimal fallback so app still works offline
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tornado Warning"] = "#FF0000",
                ["Severe Thunderstorm Warning"] = "#FFA500",
                ["Flash Flood Warning"] = "#8B0000",
                ["Extreme Wind Warning"] = "#FF8C00",
                ["Winter Weather Advisory"] = "#7B68EE",
                ["Winter Storm Warning"] = "#FF69B4",
                ["Avalanche Warning"] = "#1E90FF",
                ["Red Flag Warning"] = "#FF1493",
            };
        }
    }

    private Dictionary<string, string>? TryReadCache()
    {
        try
        {
            if (!File.Exists(_cachePath))
                return null;

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in File.ReadAllLines(_cachePath))
            {
                var parts = line.Split('|');
                if (parts.Length != 2) continue;

                var name = parts[0].Trim();
                var hex = NormalizeHex(parts[1]);

                if (name.Length > 0 && hex.Length > 0)
                    dict[name] = hex;
            }

            return dict;
        }
        catch
        {
            return null;
        }
    }

    private void WriteCache(Dictionary<string, string> palette)
    {
        try
        {
            var dir = Path.GetDirectoryName(_cachePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var lines = palette
                .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Select(kv => $"{kv.Key}|{kv.Value.TrimStart('#').ToUpperInvariant()}");

            File.WriteAllLines(_cachePath, lines);
        }
        catch
        {
            // ignore cache write failures
        }
    }

    public static Dictionary<string, string> ParseFromHelpMapHtml(string html)
    {
        // Remove scripts/styles first
        var text = Regex.Replace(html, @"<script[\s\S]*?</script>", " ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<style[\s\S]*?</style>", " ", RegexOptions.IgnoreCase);

        // Preserve structural breaks BEFORE stripping tags
        text = Regex.Replace(text, @"<\s*br\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</\s*(p|div|li)\s*>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</\s*tr\s*>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</\s*(td|th)\s*>", " | ", RegexOptions.IgnoreCase);

        // Strip remaining tags
        text = Regex.Replace(text, @"<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var hexRx = new Regex(@"(?<hex>#?[0-9A-Fa-f]{6})\b", RegexOptions.Compiled);

        using var reader = new StringReader(text);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.Length < 6) continue;

            var strict = LineRx.Match(line);
            if (strict.Success)
            {
                var name1 = strict.Groups["name"].Value.Trim();
                var hex1 = NormalizeHex(strict.Groups["hex"].Value);
                if (name1.Length > 0 && hex1.Length > 0)
                    dict[name1] = hex1;
                continue;
            }

            var matches = hexRx.Matches(line);
            if (matches.Count == 0) continue;

            var last = matches[^1];
            var hex = NormalizeHex(last.Groups["hex"].Value);
            if (hex.Length == 0) continue;

            var before = line[..last.Index].Trim();
            if (before.Length == 0) continue;

            var parts = before.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var name = parts.Length > 0 ? parts[0] : before;

            if (name.Length < 3) continue;

            dict[name] = hex;
        }

        return dict;
    }

    private static string NormalizeHex(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";

        raw = raw.Trim();
        if (raw.StartsWith("#"))
            raw = raw[1..];

        if (raw.Length != 6)
            return "";

        return "#" + raw.ToUpperInvariant();
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ThunderApp.Services;

public static class OfficialHazardPaletteLoader
{
    private static readonly object Gate = new();
    private static bool _loaded;

    /// <summary>
    /// Loads the official weather.gov Hazards Map color table from www/hazard-colors.json
    /// and pushes it into <see cref="HazardColorPalette"/>.
    /// Safe to call multiple times.
    /// </summary>
    public static void EnsureLoaded()
    {
        lock (Gate)
        {
            if (_loaded) return;

            var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "www", "hazard-colors.json");
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (File.Exists(file))
                {
                    var json = File.ReadAllText(file);
                    var doc = JsonSerializer.Deserialize<HazardColorsDoc>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (doc?.Items != null)
                    {
                        foreach (var it in doc.Items)
                        {
                            if (string.IsNullOrWhiteSpace(it.Event)) continue;
                            var hex = (it.OfficialHex ?? string.Empty).Trim();
                            if (hex.Length == 0) continue;

                            // Support 'Transparent' hazards (alpha 0)
                            if (string.Equals(it.Event.Trim(), "Child Abduction Emergency", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(it.Event.Trim(), "Blue Alert", StringComparison.OrdinalIgnoreCase))
                            {
                                // Stored as #00FFFFFF in the file, but tolerate #FFFFFF
                                if (string.Equals(hex.TrimStart('#'), "FFFFFF", StringComparison.OrdinalIgnoreCase))
                                    hex = "#00FFFFFF";
                            }

                            dict[it.Event.Trim()] = hex;
                        }
                    }
                }
            }
            catch
            {
                // If the file is missing/corrupt, fall back to an empty dict (palette will use default gray).
                dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            HazardColorPalette.SetOfficial(dict);
            _loaded = true;
        }
    }

    private sealed class HazardColorsDoc
    {
        public List<HazardColorsItem>? Items { get; set; }
    }

    private sealed class HazardColorsItem
    {
        public string? Event { get; set; }
        public string? OfficialHex { get; set; }
    }
}

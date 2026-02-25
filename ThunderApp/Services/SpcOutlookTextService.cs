using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ThunderApp.Services;

/// <summary>
/// Fetches SPC Day 1/2/3 convective outlook text (SWO) in plain-text form.
/// Uses a small disk cache to avoid repetitive downloads.
/// </summary>
public sealed class SpcOutlookTextService(HttpClient http, SimpleDiskCache disk)
{
    // Text updates a handful of times per day. Keep cache short.
    private static readonly TimeSpan DiskTtl = TimeSpan.FromMinutes(2);

    public async Task<string?> GetConvectiveOutlookTextAsync(int day)
    {
        if (day is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(day));

        // SPC hosts the outlook discussion in simple text endpoints.
        // These are consistently plain-text and avoid HTML parsing.
        string url = day switch
        {
            1 => "https://www.spc.noaa.gov/products/outlook/day1otlk.txt",
            2 => "https://www.spc.noaa.gov/products/outlook/day2otlk.txt",
            3 => "https://www.spc.noaa.gov/products/outlook/day3otlk.txt",
            _ => "https://www.spc.noaa.gov/products/outlook/day1otlk.txt"
        };

        // Disk cache key is the url.
        string? diskHit = await disk.TryReadAsync(url, DiskTtl).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(diskHit))
            return diskHit;

        try
        {
            string txt = await http.GetStringAsync(url).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(txt))
            {
                await disk.WriteAsync(url, txt).ConfigureAwait(false);
                return txt;
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }
}

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;

namespace GeoJsonWeather.Api
{
    public static class WebData
    {
        public static Task<string> SendHttpRequestAsync(string url, CancellationToken ct = default)
            => SendHttpRequestAsync(NwsDefaults.UserAgent, url, ct);

        public static async Task<string> SendHttpRequestAsync(string userAgent, string url, CancellationToken ct = default)
        {
            try
            {
                // NWS likes clear identification. Keep this stable.
                return await url
                    .WithHeader("User-Agent", string.IsNullOrWhiteSpace(userAgent) ? NwsDefaults.UserAgent : userAgent)
                    .WithHeader("Accept", "application/geo+json, application/json")
                    .GetStringAsync(cancellationToken: ct)
                    .ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                // Don’t poison downstream with null. Fail loud; caller decides fallback.
                int? status = ex.Call?.Response?.StatusCode;
                throw new HttpRequestException($"HTTP {(status.HasValue ? status.Value.ToString() : "error")} fetching {url}", ex);
            }
        }
    }

    internal static class NwsDefaults
    {
        // Put your email or site here. Doesn’t need to be fancy.
        public const string UserAgent = "ThunderWeather/1.0 (contact: you@example.com)";
    }
}
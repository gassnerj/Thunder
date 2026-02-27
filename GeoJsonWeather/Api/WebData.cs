using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;

namespace GeoJsonWeather.Api
{
    public static class WebData
    {
        // Optional hook for host app logging.
        public static Action<string>? Logger { get; set; }

        public static Task<string> SendHttpRequestAsync(string url, CancellationToken ct = default)
            => SendHttpRequestAsync(NwsDefaults.UserAgent, url, ct);

        public static async Task<string> SendHttpRequestAsync(string userAgent, string url, CancellationToken ct = default)
        {
            string ua = string.IsNullOrWhiteSpace(userAgent) ? NwsDefaults.UserAgent : userAgent;

            var sw = Stopwatch.StartNew();
            try
            {
                Logger?.Invoke($"HTTP GET {url} UA='{ua}'");

                string body = await url
                    .WithHeader("User-Agent", ua)
                    .WithHeader("Accept", "application/geo+json, application/json")
                    .WithHeader("Accept-Language", "en-US,en;q=0.9")
                    .WithTimeout(TimeSpan.FromSeconds(12))
                    .GetStringAsync(cancellationToken: ct)
                    .ConfigureAwait(false);

                Logger?.Invoke($"HTTP OK {url} ms={sw.ElapsedMilliseconds} len={body?.Length ?? 0}");
                return body;
            }
            catch (FlurlHttpTimeoutException ex)
            {
                Logger?.Invoke($"HTTP TIMEOUT {url} ms={sw.ElapsedMilliseconds} msg={ex.Message}");
                throw new TimeoutException($"Timeout fetching {url}", ex);
            }
            catch (FlurlHttpException ex)
            {
                int? status = ex.Call?.Response?.StatusCode;
                Logger?.Invoke($"HTTP FAIL {(status.HasValue ? status.Value.ToString() : "error")} {url} ms={sw.ElapsedMilliseconds} msg={ex.Message}");
                throw new HttpRequestException($"HTTP {(status.HasValue ? status.Value.ToString() : "error")} fetching {url}", ex);
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"HTTP EX {url} ms={sw.ElapsedMilliseconds} {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }
    }

    public static class NwsDefaults
    {
        // NWS asks clients to send a descriptive User-Agent with contact information.
        // Override via THUNDER_NWS_USER_AGENT if needed.
        public static string UserAgent =>
            Environment.GetEnvironmentVariable("THUNDER_NWS_USER_AGENT")
            is { Length: > 0 } explicitUa
                ? explicitUa
                : $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36 ThunderApp/1.0 (contact: {Contact})";

        private static string Contact =>
            Environment.GetEnvironmentVariable("THUNDER_NWS_CONTACT")
            is { Length: > 0 } explicitContact
                ? explicitContact
                : "thunderapp@users.noreply.github.com";
    }
}

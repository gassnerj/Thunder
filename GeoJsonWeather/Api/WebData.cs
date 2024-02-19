using System;
using System.Threading.Tasks;
using Flurl.Http;

namespace GeoJsonWeather.Api
{
    public static class WebData
    {
        public static async Task<string> SendHttpRequestAsync(string url)
        {
            return await SendHttpRequestAsync("NoUserAgent", url);
        }

        public static async Task<string> SendHttpRequestAsync(string userAgent, string url)
        {
            try
            {
                string responseString = await url
                    .WithHeader("User-Agent", userAgent)
                    .WithHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8")
                    .GetStringAsync();
                return responseString;
            }
            catch (FlurlHttpException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            return null;
        }
    }
}

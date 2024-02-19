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
                string responseString = await url.WithHeader("User-Agent", userAgent).GetStringAsync();
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

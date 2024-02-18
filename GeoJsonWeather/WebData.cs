using Flurl.Http;
using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace GeoJsonWeather
{
    public class WebData : IWebData
    {
        private readonly string _url;

        public WebData(string url)
        {
            _url = url;
        }

        public async Task<string> SendHttpRequestAsync()
        {
            return await SendHttpRequestAsync("NoUserAgent");
        }

        public async Task<string> SendHttpRequestAsync(string userAgent)
        {
            try
            {
                string responseString = await _url.WithHeader("User-Agent", userAgent).GetStringAsync();
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

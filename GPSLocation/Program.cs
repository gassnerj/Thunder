using GPSData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace GPSLocation
{

    internal class Program
    {
        private static GPS GPSLocation { get; set; }
        private const string APIKey = "AIzaSyC_opo2I9Fs8dYeVe_FPHTWlNCYpzNX3F4";
        

        static async Task Main(string[] args)
        {
            GPSLocation.MessageReceived += GPSLocation_MessageReceived;
            GPSLocation.Read();
        }

        private static void GPSLocation_MessageReceived(object? sender, GPSMessageEventArgs e)
        {
            Console.WriteLine($"{e.NMEASentence.Latitude}, {e.NMEASentence.Longitude}");
        }
    }
}
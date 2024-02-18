using System;


namespace GeoJsonWeather
{
    public static class Ticker
    {
        public static async void Tick<T>(T action, TimeSpan timeSpan)
        {
            var timer = new System.Threading.PeriodicTimer(timeSpan);

            while (await timer.WaitForNextTickAsync())
            {
                _ = action;
            }
        }
    }
}

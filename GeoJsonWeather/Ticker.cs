using System;
using System.Threading;
using System.Threading.Tasks;

namespace GeoJsonWeather
{
    public static class Ticker
    {
        public static async Task Tick(Func<Task> action, TimeSpan interval, CancellationToken ct = default)
        {
            using var timer = new PeriodicTimer(interval);

            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                await action().ConfigureAwait(false);
            }
        }
    }
}
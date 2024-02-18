using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace GeoJsonWeather
{
    public interface IFeatureCollection
    {
        ObservableCollection<IAlert> Alerts { get; set; }
        int NewAlertCount { get; set; }
        int RefreshInterval { get; set; }

        event EventHandler<AlertIssuedEventArgs> AlertIssued;
        event EventHandler<AlertMessageEventArgs> AlertMessage;

        Task FetchData(string url);
        void PurgeAlerts();
    }
}
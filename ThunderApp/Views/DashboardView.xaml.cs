using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace ThunderApp.Views
{
    public partial class DashboardView : UserControl
    {
        private bool _mapInitialized;
        private bool _mapReady;

        private double? _lastLat;
        private double? _lastLon;

        private const int DefaultZoom = 10;
        private const double MinTrailMoveMeters = 50;

        public DashboardView()
        {
            InitializeComponent();
            Loaded += DashboardView_Loaded;
        }

        private async void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_mapInitialized) return;
            _mapInitialized = true;

            await InitializeMapAsync();
        }

        private async Task InitializeMapAsync()
        {
            await MapView.EnsureCoreWebView2Async();

            var wwwRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "www");

            MapView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app",
                wwwRoot,
                CoreWebView2HostResourceAccessKind.Allow);

            MapView.CoreWebView2.WebMessageReceived += (_, args) =>
            {
                try
                {
                    var msg = args.TryGetWebMessageAsString();
                    if (string.IsNullOrWhiteSpace(msg)) return;

                    if (msg.Contains("\"type\":\"followChanged\"") &&
                        msg.Contains("\"value\":false"))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (FollowToggle != null)
                                FollowToggle.IsChecked = false;
                        });
                    }
                }
                catch { }
            };

            MapView.NavigationCompleted += async (_, __) =>
            {
                _mapReady = true;

                const string test = """
                                    {
                                      "type": "FeatureCollection",
                                      "features": [
                                        {
                                          "type": "Feature",
                                          "properties": { "event":"Test Polygon", "headline":"Hello", "severity":"Severe", "affectsMe": true },
                                          "geometry": {
                                            "type": "Polygon",
                                            "coordinates": [[
                                              [-98.70, 33.98],
                                              [-98.60, 33.98],
                                              [-98.60, 33.92],
                                              [-98.70, 33.92],
                                              [-98.70, 33.98]
                                            ]]
                                          }
                                        }
                                      ]
                                    }
                                    """;
                await SetAlertPolygonsAsync(test);
                
                await UpdateMapLocationAsync(
                    33.9696284,
                    -98.6710611,
                    DefaultZoom,
                    addTrail: false,
                    forceCenter: true);
            };

            MapView.Source = new Uri("https://app/map.html");
        }

        private async void Radar_Checked(object sender, RoutedEventArgs e)
        {
            if (!_mapReady || MapView?.CoreWebView2 == null) return;
            await MapView.CoreWebView2.ExecuteScriptAsync("setRadarEnabled(true);");
        }

        private async void Radar_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_mapReady || MapView?.CoreWebView2 == null) return;
            await MapView.CoreWebView2.ExecuteScriptAsync("setRadarEnabled(false);");
        }
        
        private async void UpdateMap_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_mapReady) return;

            var latText = ManualLatTextBox.Text?.Trim();
            var lonText = ManualLonTextBox.Text?.Trim();

            if (!double.TryParse(latText, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
                return;

            if (!double.TryParse(lonText, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                return;

            FollowToggle.IsChecked = true;

            await UpdateMapLocationAsync(lat, lon, DefaultZoom, addTrail: true, forceCenter: true);
        }

        public async Task UpdateMapLocationAsync(
            double lat,
            double lon,
            int zoom = DefaultZoom,
            bool addTrail = true,
            bool forceCenter = false)
        {
            if (!_mapReady || MapView?.CoreWebView2 == null)
                return;

            if (addTrail && _lastLat.HasValue && _lastLon.HasValue)
            {
                var meters = HaversineMeters(_lastLat.Value, _lastLon.Value, lat, lon);
                if (meters < MinTrailMoveMeters)
                    addTrail = false;
            }

            _lastLat = lat;
            _lastLon = lon;

            var latStr = lat.ToString(CultureInfo.InvariantCulture);
            var lonStr = lon.ToString(CultureInfo.InvariantCulture);

            bool follow = FollowToggle?.IsChecked == true;

            await MapView.CoreWebView2.ExecuteScriptAsync(
                $"updateGps({latStr}, {lonStr}, {zoom}, {(addTrail ? "true" : "false")});");

            if (forceCenter && !follow)
            {
                await MapView.CoreWebView2.ExecuteScriptAsync(
                    $"setView({latStr}, {lonStr}, {zoom});");
            }
        }

        private async void FollowToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (!_mapReady || MapView?.CoreWebView2 == null) return;
            await MapView.CoreWebView2.ExecuteScriptAsync("setFollow(true);");
        }

        private async void FollowToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_mapReady || MapView?.CoreWebView2 == null) return;
            await MapView.CoreWebView2.ExecuteScriptAsync("setFollow(false);");
        }

        private async void ClearTrail_OnClick(object sender, RoutedEventArgs e)
        {
            _lastLat = null;
            _lastLon = null;

            if (!_mapReady || MapView?.CoreWebView2 == null) return;
            await MapView.CoreWebView2.ExecuteScriptAsync("clearTrail();");
        }

        private static double HaversineMeters(
            double lat1,
            double lon1,
            double lat2,
            double lon2)
        {
            const double R = 6371000;
            static double ToRad(double deg) => deg * (Math.PI / 180.0);

            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);

            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
        
        public async Task SetAlertPolygonsAsync(string geoJsonFeatureCollection)
        {
            if (!_mapReady || MapView?.CoreWebView2 == null) return;

            // safest: send via WebMessage to avoid quote escaping
            MapView.CoreWebView2.PostWebMessageAsString(geoJsonFeatureCollection);
        }
    }
}
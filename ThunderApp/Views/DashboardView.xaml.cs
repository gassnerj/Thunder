using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GeoJsonWeather.Api;
using Microsoft.Web.WebView2.Core;
using ThunderApp.Models;
using ThunderApp.Services;
using ThunderApp.ViewModels;

namespace ThunderApp.Views
{
    public partial class DashboardView : UserControl
    {
        private static readonly HttpClient _http = new();
        private static readonly SimpleDiskCache _zoneDisk =
            new(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache", "zones"));
        private static readonly NwsZoneGeometryService _zoneSvc = new(_http, _zoneDisk);
        private bool _mapInitialized;
        private bool _mapReady;

        private double? _lastLat;
        private double? _lastLon;

        private const int DefaultZoom = 10;
        private const double MinTrailMoveMeters = 5;

        private DashboardViewModel? _vm;

        private PropertyChangedEventHandler? _filterChangedHandler;
        private PropertyChangedEventHandler? _vmChangedHandler;
        private static bool _paletteHooked;


        private bool _splitApplied;
        
        private int _alertsUpdateVersion;

        private SpcOutlookTextWindow? _spcTextWindow;

        public DashboardView()
        {
            InitializeComponent();

            EnsureNwsHeaders();

            Loaded += DashboardView_Loaded;
            DataContextChanged += DashboardView_DataContextChanged;
        }

        private async void SpcText_OnClick(object sender, RoutedEventArgs e)
        {
            if (_vm == null) return;

            // Fetch latest text on demand, then show.
            try
            {
                await _vm.RefreshSpcTextAsync();
            }
            catch
            {
                // VM sets status/logs.
            }

            if (_spcTextWindow == null || !_spcTextWindow.IsVisible)
            {
                _spcTextWindow = new SpcOutlookTextWindow
                {
                    Owner = Window.GetWindow(this),
                    DataContext = _vm
                };
                _spcTextWindow.Closed += (_, _) => _spcTextWindow = null;
                _spcTextWindow.Show();
            }
            else
            {
                _spcTextWindow.Activate();
            }
        }

        private static void EnsureNwsHeaders()
        {
            // NWS expects a User-Agent, and behaves better with geo+json accept header.
            if (!_http.DefaultRequestHeaders.UserAgent.Any())
            {
                _http.DefaultRequestHeaders.UserAgent.ParseAdd(NwsDefaults.UserAgent);
                _http.DefaultRequestHeaders.Accept.Clear();
                _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/geo+json"));
                _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
        }

        private void DashboardView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vm?.Alerts is INotifyCollectionChanged oldObs)
                oldObs.CollectionChanged -= Alerts_CollectionChanged;

            if (_vm != null)
                _vm.AlertsChanged -= Vm_AlertsChanged;

            if (_vm != null && _vmChangedHandler != null)
                _vm.PropertyChanged -= _vmChangedHandler;

            if (_vm?.FilterSettings != null && _filterChangedHandler != null)
                _vm.FilterSettings.PropertyChanged -= _filterChangedHandler;

            _vm = DataContext as DashboardViewModel;

            if (_vm?.Alerts is INotifyCollectionChanged obs)
                obs.CollectionChanged += Alerts_CollectionChanged;

            if (_vm != null)
                _vm.AlertsChanged += Vm_AlertsChanged;

            if (_vm != null)
            {
                _vmChangedHandler = (_, args) =>
                {
                    if (args.PropertyName == nameof(DashboardViewModel.CurrentLocation))
                    {
                        _ = Dispatcher.BeginInvoke(new Action(async () =>
                        {
                            try
                            {
                                var p = _vm?.CurrentLocation;
                                if (p != null)
                                    await UpdateMapLocationAsync(p.Value.Lat, p.Value.Lon, DefaultZoom, addTrail: true);
                            }
                            catch { }
                        }));
                    }
                };
                _vm.PropertyChanged += _vmChangedHandler;
            }

            if (_vm?.FilterSettings != null)
            {
                _filterChangedHandler = (_, args) =>
                {
                    if (args.PropertyName is nameof(AlertFilterSettings.UseRadiusFilter)
                        or nameof(AlertFilterSettings.RadiusMiles)
                        or nameof(AlertFilterSettings.ShowRadiusCircle)
                        or nameof(AlertFilterSettings.UseNearMe)
                        or nameof(AlertFilterSettings.ManualLat)
                        or nameof(AlertFilterSettings.ManualLon))
                    {
                        _ = UpdateRangeCircleOnMapAsync();
                    }

                    if (args.PropertyName is nameof(AlertFilterSettings.ShowSpcDay1)
                        or nameof(AlertFilterSettings.ShowSpcDay2)
                        or nameof(AlertFilterSettings.ShowSpcDay3))
                    {
                        _ = UpdateSpcOverlaysOnMapAsync();
                    }

                    if (args.PropertyName is nameof(AlertFilterSettings.MapSplitRatio))
                    {
                        ApplySavedAlertsMapSplit();
                _ = UpdateMapStylingOnMapAsync();
                    }

                    if (args.PropertyName is nameof(AlertFilterSettings.ShowSeverityOutline)
                        or nameof(AlertFilterSettings.ShowSeverityGlow)
                        or nameof(AlertFilterSettings.ShowSeverityStripes)
                        or nameof(AlertFilterSettings.HazardPaletteMode)
                        or nameof(AlertFilterSettings.CustomHazardColors)
                        or nameof(AlertFilterSettings.AlertsOpacityPercent)
                        or nameof(AlertFilterSettings.SpcOpacityPercent))
                    {
                        _ = UpdateMapStylingOnMapAsync();
                    }
                };

                _vm.FilterSettings.PropertyChanged += _filterChangedHandler;

                if (!_paletteHooked)
                {
                    _paletteHooked = true;
                    HazardColorPalette.PaletteChanged += () => _ = UpdateMapStylingOnMapAsync();
                }

                // Apply split once we have settings.
                ApplySavedAlertsMapSplit();
            }
        }

        private async Task UpdateSeverityOutlineOnMapAsync()
        {
            if (!_mapReady || MapView?.CoreWebView2 == null) return;
            _vm ??= DataContext as DashboardViewModel;
            if (_vm?.FilterSettings == null) return;

            await MapView.CoreWebView2.ExecuteScriptAsync($"setSeverityOutline({(_vm.FilterSettings.ShowSeverityOutline ? "true" : "false")});");
        }

        
        private async Task UpdateMapStylingOnMapAsync()
        {
            if (!_mapReady || MapView?.CoreWebView2 == null) return;
            _vm ??= DataContext as DashboardViewModel;
            if (_vm?.FilterSettings == null) return;

            // Hazard palette (effective)
            try
            {
                var palette = ThunderApp.Services.HazardColorPalette.GetEffectivePalette();
                var json = System.Text.Json.JsonSerializer.Serialize(palette);
                await MapView.CoreWebView2.ExecuteScriptAsync($"setHazardPalette({json});");
            }
            catch { }

            // Severity visuals
            try
            {
                await MapView.CoreWebView2.ExecuteScriptAsync($"setSeverityOutline({(_vm.FilterSettings.ShowSeverityOutline ? "true" : "false")});");
                await MapView.CoreWebView2.ExecuteScriptAsync($"setSeverityGlow({(_vm.FilterSettings.ShowSeverityGlow ? "true" : "false")});");
                await MapView.CoreWebView2.ExecuteScriptAsync($"setSeverityStripes({(_vm.FilterSettings.ShowSeverityStripes ? "true" : "false")});");
            }
            catch { }


            // Layer opacity
            try
            {
                double a = Math.Clamp(_vm.FilterSettings.AlertsOpacityPercent / 100.0, 0.0, 1.0);
                double s = Math.Clamp(_vm.FilterSettings.SpcOpacityPercent / 100.0, 0.0, 1.0);
                await MapView.CoreWebView2.ExecuteScriptAsync($"setAlertsOpacity({a.ToString(System.Globalization.CultureInfo.InvariantCulture)});");
                await MapView.CoreWebView2.ExecuteScriptAsync($"setSpcOpacity({s.ToString(System.Globalization.CultureInfo.InvariantCulture)});");
            }
            catch { }
        }

private void ApplySavedAlertsMapSplit()
        {
            if (RightPaneGrid?.RowDefinitions == null || RightPaneGrid.RowDefinitions.Count < 3) return;

            _vm ??= DataContext as DashboardViewModel;
            if (_vm?.FilterSettings == null) return;

            if (_splitApplied && !_mapReady) return;

            double r = _vm.FilterSettings.MapSplitRatio;
            if (double.IsNaN(r) || double.IsInfinity(r)) r = 0.50;
            r = Math.Max(0.15, Math.Min(0.85, r));

            // Row 0 = alerts; Row 2 = map
            RightPaneGrid.RowDefinitions[0].Height = new GridLength(1.0 - r, GridUnitType.Star);
            RightPaneGrid.RowDefinitions[2].Height = new GridLength(r, GridUnitType.Star);

            _splitApplied = true;
        }

        private async Task UpdateSpcOverlaysOnMapAsync()
        {
            if (!_mapReady || MapView?.CoreWebView2 == null) return;

            _vm ??= DataContext as DashboardViewModel;
            if (_vm?.FilterSettings == null) return;

            // day1/day2/day3 categorical overlays
            await MapView.CoreWebView2.ExecuteScriptAsync($"setSpcOutlook('day1', {(_vm.FilterSettings.ShowSpcDay1 ? "true" : "false")});");
            await MapView.CoreWebView2.ExecuteScriptAsync($"setSpcOutlook('day2', {(_vm.FilterSettings.ShowSpcDay2 ? "true" : "false")});");
            await MapView.CoreWebView2.ExecuteScriptAsync($"setSpcOutlook('day3', {(_vm.FilterSettings.ShowSpcDay3 ? "true" : "false")});");
        }

        private void AlertsMapSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            try
            {
                _vm ??= DataContext as DashboardViewModel;
                if (_vm?.FilterSettings == null) return;

                if (RightPaneGrid?.RowDefinitions == null || RightPaneGrid.RowDefinitions.Count < 3) return;

                double alertsH = RightPaneGrid.RowDefinitions[0].ActualHeight;
                double mapH = RightPaneGrid.RowDefinitions[2].ActualHeight;
                double total = alertsH + mapH;
                if (total < 50) return;

                double ratio = mapH / total;
                ratio = Math.Max(0.15, Math.Min(0.85, ratio));

                _vm.FilterSettings.MapSplitRatio = ratio;

                // Persist immediately so it survives a crash while driving.
                if (_vm.SaveFiltersCommand?.CanExecute(null) == true)
                    _vm.SaveFiltersCommand.Execute(null);
            }
            catch
            {
            }
        }

        private void Vm_AlertsChanged(object? sender, EventArgs e)
        {
            _ = QueueAlertPolygonUpdateAsync();
        }

        private void Alerts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            _ = QueueAlertPolygonUpdateAsync();
        }
        
        private async Task QueueAlertPolygonUpdateAsync()
        {
            if (!_mapReady) return;

            int myVersion = System.Threading.Interlocked.Increment(ref _alertsUpdateVersion);

            // Small debounce: if a bunch of events fire, let them coalesce.
            await Task.Delay(150);

            // If a newer request arrived during the delay, bail.
            if (myVersion != _alertsUpdateVersion) return;

            var geojson = await BuildAlertGeoJsonFromFilteredViewAsync();

            // If another update started while we were building, don't apply stale data.
            if (myVersion != _alertsUpdateVersion) return;

            await SetAlertPolygonsAsync(geojson);
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

            string wwwRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "www");

            MapView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app",
                wwwRoot,
                CoreWebView2HostResourceAccessKind.Allow);

            MapView.CoreWebView2.WebMessageReceived += async (_, args) =>
            {
                try
                {
                    string? msg = args.TryGetWebMessageAsString();

                    if (string.IsNullOrWhiteSpace(msg)) return;

                    if (msg.Contains("\"type\":\"followChanged\"") &&
                        msg.Contains("\"value\":false"))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (FollowToggle != null)
                                FollowToggle.IsChecked = false;
                        });
                        return;
                    }

                    using JsonDocument doc = JsonDocument.Parse(msg);
                    JsonElement root = doc.RootElement;

                    string? type = root.TryGetProperty("type", out var t) ? t.GetString() : null;
                    if (string.IsNullOrWhiteSpace(type)) return;

                    if (type == "polygonClicked")
                    {
                        string? id = root.GetProperty("id").GetString();
                        if (string.IsNullOrWhiteSpace(id)) return;

                        // Clicking a polygon should only select the corresponding alert.
                        // Routing is initiated explicitly via the "Route" button.
                        await Dispatcher.InvokeAsync(() => SelectAlertById(id));
                        return;
                    }

                    if (type == "mapClick" || type == "mapDblClick")
                    {
                        if (!root.TryGetProperty("lat", out var latEl) || !root.TryGetProperty("lon", out var lonEl))
                            return;

                        double lat = latEl.GetDouble();
                        double lon = lonEl.GetDouble();

                        await Dispatcher.InvokeAsync(async () =>
                        {
                            _vm ??= DataContext as DashboardViewModel;
                            if (_vm == null) return;

                            _vm.MapClickCoordsText = $"Map: {lat:0.#####}, {lon:0.#####}  (double-click to set)";

                            if (type == "mapDblClick")
                            {
                                // Move "manual location" to the double-clicked point.
                                _vm.ManualCoordsText = $"{lat:0.##########}, {lon:0.##########}";
                                _vm.SetCurrentLocation(lat, lon);

                                if (FollowToggle != null)
                                    FollowToggle.IsChecked = true;

                                await UpdateMapLocationAsync(lat, lon, DefaultZoom, addTrail: false, forceCenter: true);
                            }
                        });

                        return;
                    }

                    if (type == "spcError")
                    {
                        string day = root.TryGetProperty("day", out var d) ? (d.GetString() ?? "") : "";
                        string message = root.TryGetProperty("message", out var m) ? (m.GetString() ?? "") : "";
                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (_vm != null)
                                _vm.StatusText = $"SPC overlay error ({day}): {message}";
                        });
                        return;
                    }
                }
                catch
                {
                }
            };

            MapView.NavigationCompleted += (o, __) =>
            {
                _mapReady = true;
                ApplySavedAlertsMapSplit();
                _ = QueueAlertPolygonUpdateAsync();
                _ = UpdateRangeCircleOnMapAsync();
                _ = UpdateSpcOverlaysOnMapAsync();
                // Push palette + severity visuals immediately on first load.
                _ = UpdateMapStylingOnMapAsync();
            };

            MapView.Source = new Uri("https://app/map.html");
        }

        private void SelectAlertById(string alertId)
        {
            _vm ??= DataContext as DashboardViewModel;
            if (_vm == null) return;

            var match = _vm.Alerts.FirstOrDefault(a =>
                string.Equals(a.Id, alertId, StringComparison.OrdinalIgnoreCase));

            if (match != null)
                _vm.SelectedAlert = match;
        }

        private async void Route_OnClick(object sender, RoutedEventArgs e)
        {
            _vm ??= DataContext as DashboardViewModel;
            if (_vm?.SelectedAlert?.Id == null) return;
            await RouteToAlertAsync(_vm.SelectedAlert.Id);
        }

        private async Task RouteToAlertAsync(string alertId)
        {
            _vm ??= DataContext as DashboardViewModel;
            if (_vm == null) return;

            if (!_lastLat.HasValue || !_lastLon.HasValue)
            {
                // Filters live in a separate window now, so don't depend on hidden textboxes.
                // Use the VM settings as the source of truth.
                var fs = _vm.FilterSettings;
                double mLat = fs.ManualLat;
                double mLon = fs.ManualLon;

                if (!double.IsNaN(mLat) && !double.IsNaN(mLon) && mLat != 0 && mLon != 0)
                {
                    _lastLat = mLat;
                    _lastLon = mLon;

                    if (_mapReady && MapView?.CoreWebView2 != null)
                    {
                        var mLatStr = mLat.ToString(CultureInfo.InvariantCulture);
                        var mLonStr = mLon.ToString(CultureInfo.InvariantCulture);
                        await MapView.CoreWebView2.ExecuteScriptAsync($"updateGps({mLatStr}, {mLonStr}, {DefaultZoom}, false);");
                    }
                }
                else
                {
                    return;
                }
            }

            NwsAlert? alert = _vm.Alerts.FirstOrDefault(a =>
                string.Equals(a.Id, alertId, StringComparison.OrdinalIgnoreCase));

            if (alert == null) return;
            if (string.IsNullOrWhiteSpace(alert.GeometryJson)) return;

            if (!TryPickRoutePoint(
                    alert.GeometryJson,
                    _lastLat.Value,
                    _lastLon.Value,
                    out double destLat,
                    out double destLon))
                return;

            if (!_mapReady || MapView?.CoreWebView2 == null)
                return;

            var latStr = destLat.ToString(CultureInfo.InvariantCulture);
            var lonStr = destLon.ToString(CultureInfo.InvariantCulture);

            await MapView.CoreWebView2.ExecuteScriptAsync($"setRouteDestination({latStr}, {lonStr});");
        }

        private static bool TryPickRoutePoint(
            string geometryJson,
            double fromLat,
            double fromLon,
            out double lat,
            out double lon)
        {
            lat = 0;
            lon = 0;

            try
            {
                using JsonDocument doc = JsonDocument.Parse(geometryJson);
                JsonElement root = doc.RootElement;

                string type = root.GetProperty("type").GetString() ?? "";

                var ring = type switch
                {
                    "Polygon" => ExtractLargestRingFromPolygon(root),
                    "MultiPolygon" => ExtractLargestRingFromMultiPolygon(root),
                    _ => new List<(double Lon, double Lat)>()
                };

                if (ring.Count == 0)
                    return false;

                var bestDist = double.MaxValue;
                (double Lon, double Lat) best = ring[0];

                foreach ((double Lon, double Lat) p in ring)
                {
                    double d = HaversineMeters(fromLat, fromLon, p.Lat, p.Lon);
                    if (!(d < bestDist)) continue;
                    bestDist = d;
                    best = p;
                }

                lat = best.Lat;
                lon = best.Lon;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static List<(double Lon, double Lat)> ExtractLargestRingFromPolygon(JsonElement geom)
        {
            JsonElement coords = geom.GetProperty("coordinates");
            if (coords.ValueKind != JsonValueKind.Array) return [];

            double bestArea = double.NegativeInfinity;
            List<(double Lon, double Lat)> best = [];

            foreach (JsonElement ring in coords.EnumerateArray())
            {
                var pts = ExtractRing(ring);
                double area = Math.Abs(SignedArea(pts));
                if (!(area > bestArea)) continue;
                bestArea = area;
                best = pts;
            }

            return best;
        }

        private static List<(double Lon, double Lat)> ExtractLargestRingFromMultiPolygon(JsonElement geom)
        {
            JsonElement coords = geom.GetProperty("coordinates");
            if (coords.ValueKind != JsonValueKind.Array) return [];

            double bestArea = double.NegativeInfinity;
            List<(double Lon, double Lat)> best = [];

            foreach (JsonElement poly in coords.EnumerateArray())
            {
                if (poly.ValueKind != JsonValueKind.Array) continue;

                foreach (JsonElement ring in poly.EnumerateArray())
                {
                    var pts = ExtractRing(ring);
                    double area = Math.Abs(SignedArea(pts));
                    if (!(area > bestArea)) continue;
                    bestArea = area;
                    best = pts;
                }
            }

            return best;
        }

        private static List<(double Lon, double Lat)> ExtractRing(JsonElement ring)
        {
            var pts = new List<(double Lon, double Lat)>();

            if (ring.ValueKind != JsonValueKind.Array)
                return pts;

            foreach (JsonElement p in ring.EnumerateArray())
            {
                if (p.ValueKind != JsonValueKind.Array) continue;
                if (p.GetArrayLength() < 2) continue;

                double lon = p[0].GetDouble();
                double lat = p[1].GetDouble();
                pts.Add((lon, lat));
            }

            return pts;
        }

        private static double SignedArea(List<(double Lon, double Lat)> ring)
        {
            if (ring.Count < 3) return 0;
            double a = 0;

            for (var i = 0; i < ring.Count; i++)
            {
                (double x1, double y1) = ring[i];
                (double x2, double y2) = ring[(i + 1) % ring.Count];
                a += (x1 * y2) - (x2 * y1);
            }

            return a / 2.0;
        }

        private async void Radar_Checked(object sender, RoutedEventArgs e)
        {
            if (!_mapReady || MapView?.CoreWebView2 == null) return;
            await MapView.CoreWebView2.ExecuteScriptAsync("setRadarEnabled(true);");
            try
            {
                int n = (int)Math.Round(RadarFramesSlider?.Value ?? 10);
                await MapView.CoreWebView2.ExecuteScriptAsync($"window.radar && radar.setFrameCount({n});");
            }
            catch { }

        }

        private async void Radar_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_mapReady || MapView?.CoreWebView2 == null) return;
            await MapView.CoreWebView2.ExecuteScriptAsync("setRadarEnabled(false);");
        }


private async void RadarFrames_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
{
    if (!_mapReady || MapView?.CoreWebView2 == null) return;

    // Avoid spamming JS while the control is initializing.
    int n = (int)Math.Round(e.NewValue);
    await MapView.CoreWebView2.ExecuteScriptAsync($"window.radar && radar.setFrameCount({n});");
}
        private async void UpdateMap_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_mapReady) return;

            // Manual coordinates are MVVM-bound (single textbox) and parsed by the ViewModel.
            // So here we simply use the ViewModel's current location.
            if (DataContext is ThunderApp.ViewModels.DashboardViewModel vm && vm.CurrentLocation is ThunderApp.Models.GeoPoint p)
            {
                FollowToggle.IsChecked = true;
                vm.SetCurrentLocation(p.Lat, p.Lon); // also wakes fast refresh loop
                await UpdateMapLocationAsync(p.Lat, p.Lon, DefaultZoom, addTrail: true, forceCenter: true);
            }

            _ = QueueAlertPolygonUpdateAsync();
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
                double meters = HaversineMeters(_lastLat.Value, _lastLon.Value, lat, lon);
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

            await UpdateRangeCircleOnMapAsync();

            if (forceCenter && !follow)
            {
                await MapView.CoreWebView2.ExecuteScriptAsync($"setView({latStr}, {lonStr}, {zoom});");
            }
        }

        private async Task UpdateRangeCircleOnMapAsync()
        {
            if (!_mapReady || MapView?.CoreWebView2 == null) return;

            _vm ??= DataContext as DashboardViewModel;
            if (_vm == null) return;

            // Display-only toggle (do NOT tie this to filtering behavior)
            bool enabled = _vm.FilterSettings.ShowRadiusCircle;
            double miles = _vm.FilterSettings.RadiusMiles;

            // Center circle on GPS if available, otherwise manual.
            double lat, lon;
            if (_lastLat.HasValue && _lastLon.HasValue)
            {
                lat = _lastLat.Value;
                lon = _lastLon.Value;
            }
            else
            {
                lat = _vm.FilterSettings.ManualLat;
                lon = _vm.FilterSettings.ManualLon;
            }

            var latStr = lat.ToString(CultureInfo.InvariantCulture);
            var lonStr = lon.ToString(CultureInfo.InvariantCulture);
            var milesStr = miles.ToString(CultureInfo.InvariantCulture);
            var enabledStr = enabled ? "true" : "false";

            await MapView.CoreWebView2.ExecuteScriptAsync(
                $"setRangeCircle({latStr}, {lonStr}, {enabledStr}, {milesStr});");
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

        private async void ClearRoute_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_mapReady || MapView?.CoreWebView2 == null) return;
            await MapView.CoreWebView2.ExecuteScriptAsync("clearRoute();");
        }

        private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000;
            static double ToRad(double deg) => deg * (Math.PI / 180.0);

            double dLat = ToRad(lat2 - lat1);
            double dLon = ToRad(lon2 - lon1);

            double a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private Task SetAlertPolygonsAsync(string geoJsonFeatureCollection)
        {
            if (!_mapReady || MapView?.CoreWebView2 == null) return Task.CompletedTask;
            MapView.CoreWebView2.PostWebMessageAsString(geoJsonFeatureCollection);
            return Task.CompletedTask;
        }

        private async Task<string> BuildAlertGeoJsonFromFilteredViewAsync()
        {
            if (!_mapReady) return "{\"type\":\"FeatureCollection\",\"features\":[]}";
            _vm ??= DataContext as DashboardViewModel;
            if (_vm?.AlertsView == null) return "{\"type\":\"FeatureCollection\",\"features\":[]}";

            var visibleAlerts = _vm.AlertsView
                .Cast<object>()
                .OfType<NwsAlert>()
                .ToList();

            var expanded = new List<NwsAlert>();

            foreach (var a in visibleAlerts)
            {
                if (!string.IsNullOrWhiteSpace(a.GeometryJson))
                {
                    expanded.Add(a);
                    continue;
                }

                if (a.AffectedZonesUrls is not { Count: > 0 })
                    continue;

                var zoneUrls = a.AffectedZonesUrls
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (zoneUrls.Count == 0)
                    continue;

                var zoneGeoms = new List<string>(zoneUrls.Count);

                foreach (var z in zoneUrls)
                {
                    var zg = await _zoneSvc.GetGeometryJsonAsync(z);
                    if (string.IsNullOrWhiteSpace(zg)) continue;
                    zoneGeoms.Add(zg);
                }

                if (zoneGeoms.Count == 0)
                    continue;

                var merged = BuildMergedMultiPolygonGeometry(zoneGeoms);
                if (string.IsNullOrWhiteSpace(merged))
                    continue;

                expanded.Add(new NwsAlert(
                    a.Id,
                    a.Event,
                    a.Headline,
                    a.Severity,
                    a.Urgency,
                    a.Effective,
                    a.Expires,
                    a.Ends,
                    a.Onset,
                    a.AreaDescription,
                    a.SenderName,
                    a.Description,
                    a.Instruction,
                    merged,
                    null
                ));
            }

            return BuildGeoJsonFeatureCollection(expanded);
        }

        private static string? BuildMergedMultiPolygonGeometry(IReadOnlyList<string> geometries)
        {
            try
            {
                var polygonCoordElements = new List<JsonElement>();
                var docs = new List<JsonDocument>();

                try
                {
                    foreach (string g in geometries)
                    {
                        JsonDocument d = JsonDocument.Parse(g);
                        docs.Add(d);

                        JsonElement root = d.RootElement;
                        if (!root.TryGetProperty("type", out JsonElement t)) continue;

                        string? type = t.GetString();

                        if (string.Equals(type, "Polygon", StringComparison.OrdinalIgnoreCase))
                        {
                            polygonCoordElements.Add(root.GetProperty("coordinates"));
                        }
                        else if (string.Equals(type, "MultiPolygon", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (JsonElement poly in root.GetProperty("coordinates").EnumerateArray())
                                polygonCoordElements.Add(poly);
                        }
                    }

                    if (polygonCoordElements.Count == 0)
                        return null;

                    using var ms = new MemoryStream();
                    using var w = new Utf8JsonWriter(ms);

                    w.WriteStartObject();
                    w.WriteString("type", "MultiPolygon");
                    w.WritePropertyName("coordinates");
                    w.WriteStartArray();

                    foreach (JsonElement polyCoords in polygonCoordElements)
                        polyCoords.WriteTo(w);

                    w.WriteEndArray();
                    w.WriteEndObject();
                    w.Flush();

                    return Encoding.UTF8.GetString(ms.ToArray());
                }
                finally
                {
                    foreach (JsonDocument d in docs) d.Dispose();
                }
            }
            catch
            {
                return null;
            }
        }

        private static string BuildGeoJsonFeatureCollection(IReadOnlyList<NwsAlert> alerts)
        {
            using var ms = new MemoryStream();
            using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = false });

            writer.WriteStartObject();
            writer.WriteString("type", "FeatureCollection");
            writer.WritePropertyName("features");
            writer.WriteStartArray();

            foreach (NwsAlert a in alerts)
            {
                if (string.IsNullOrWhiteSpace(a.GeometryJson))
                    continue;

                JsonDocument? geomDoc = null;
                try
                {
                    geomDoc = JsonDocument.Parse(a.GeometryJson);
                }
                catch
                {
                    continue;
                }

                writer.WriteStartObject();
                writer.WriteString("type", "Feature");

                writer.WritePropertyName("properties");
                writer.WriteStartObject();
                writer.WriteString("id", a.Id ?? "");
                writer.WriteString("event", a.Event ?? "");
                writer.WriteString("headline", a.Headline ?? "");
                writer.WriteString("severity", a.Severity ?? "Unknown");
                writer.WriteString("urgency", a.Urgency ?? "");
                writer.WriteEndObject();

                writer.WritePropertyName("geometry");
                geomDoc.RootElement.WriteTo(writer);

                writer.WriteEndObject();

                geomDoc.Dispose();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}
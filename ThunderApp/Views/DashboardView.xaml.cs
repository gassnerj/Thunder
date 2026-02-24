using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using Microsoft.Web.WebView2.Core;
using ThunderApp.Models;
using ThunderApp.Services;
using ThunderApp.ViewModels;

namespace ThunderApp.Views
{
    public partial class DashboardView : UserControl
    {
        private static readonly HttpClient _http = new();
        private static readonly NwsZoneGeometryService _zoneSvc = new(_http);
        private bool _mapInitialized;
        private bool _mapReady;

        private double? _lastLat;
        private double? _lastLon;

        private const int DefaultZoom = 10;
        private const double MinTrailMoveMeters = 50;

        private DashboardViewModel? _vm;

        public DashboardView()
        {
            InitializeComponent();

            EnsureNwsHeaders();

            Loaded += DashboardView_Loaded;
            DataContextChanged += DashboardView_DataContextChanged;
        }

        private static void EnsureNwsHeaders()
        {
            // NWS expects a User-Agent, and behaves better with geo+json accept header.
            if (!_http.DefaultRequestHeaders.UserAgent.Any())
            {
                _http.DefaultRequestHeaders.UserAgent.ParseAdd("ThunderApp/1.0 (contact: you@example.com)");
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

            _vm = DataContext as DashboardViewModel;

            if (_vm?.Alerts is INotifyCollectionChanged obs)
                obs.CollectionChanged += Alerts_CollectionChanged;

            if (_vm != null)
                _vm.AlertsChanged += Vm_AlertsChanged;
        }

        private async void Vm_AlertsChanged(object? sender, EventArgs e)
        {
            await UpdateAlertPolygonsFromFilteredViewAsync();
        }

        private async void Alerts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            await UpdateAlertPolygonsFromFilteredViewAsync();
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

            MapView.CoreWebView2.WebMessageReceived += async (_, args) =>
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
                        return;
                    }

                    if (msg.Contains("\"type\":\"polygonClicked\""))
                    {
                        using var doc = JsonDocument.Parse(msg);
                        var root = doc.RootElement;

                        var id = root.GetProperty("id").GetString();
                        if (string.IsNullOrWhiteSpace(id)) return;

                        await Dispatcher.InvokeAsync(async () => { await RouteToAlertAsync(id); });
                        return;
                    }
                }
                catch
                {
                }
            };

            MapView.NavigationCompleted += async (_, __) =>
            {
                _mapReady = true;
                await UpdateAlertPolygonsFromFilteredViewAsync();
            };

            MapView.Source = new Uri("https://app/map.html");
        }

        private async Task RouteToAlertAsync(string alertId)
        {
            if (_vm == null) _vm = DataContext as DashboardViewModel;
            if (_vm == null) return;

            if (!_lastLat.HasValue || !_lastLon.HasValue)
            {
                if (double.TryParse(ManualLatTextBox.Text?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var mLat) &&
                    double.TryParse(ManualLonTextBox.Text?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var mLon))
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

            var alert = _vm.Alerts.FirstOrDefault(a =>
                string.Equals(a.Id, alertId, StringComparison.OrdinalIgnoreCase));

            if (alert == null) return;
            if (string.IsNullOrWhiteSpace(alert.GeometryJson)) return;

            if (!TryPickRoutePoint(
                    alert.GeometryJson,
                    _lastLat.Value,
                    _lastLon.Value,
                    out var destLat,
                    out var destLon))
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
                using var doc = JsonDocument.Parse(geometryJson);
                var root = doc.RootElement;

                var type = root.GetProperty("type").GetString() ?? "";

                var ring = type switch
                {
                    "Polygon" => ExtractLargestRingFromPolygon(root),
                    "MultiPolygon" => ExtractLargestRingFromMultiPolygon(root),
                    _ => new List<(double Lon, double Lat)>()
                };

                if (ring.Count == 0)
                    return false;

                double bestDist = double.MaxValue;
                (double Lon, double Lat) best = ring[0];

                foreach (var p in ring)
                {
                    var d = HaversineMeters(fromLat, fromLon, p.Lat, p.Lon);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = p;
                    }
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
            var coords = geom.GetProperty("coordinates");
            if (coords.ValueKind != JsonValueKind.Array) return [];

            double bestArea = double.NegativeInfinity;
            List<(double Lon, double Lat)> best = [];

            foreach (var ring in coords.EnumerateArray())
            {
                var pts = ExtractRing(ring);
                var area = Math.Abs(SignedArea(pts));
                if (area > bestArea)
                {
                    bestArea = area;
                    best = pts;
                }
            }

            return best;
        }

        private static List<(double Lon, double Lat)> ExtractLargestRingFromMultiPolygon(JsonElement geom)
        {
            var coords = geom.GetProperty("coordinates");
            if (coords.ValueKind != JsonValueKind.Array) return [];

            double bestArea = double.NegativeInfinity;
            List<(double Lon, double Lat)> best = [];

            foreach (var poly in coords.EnumerateArray())
            {
                if (poly.ValueKind != JsonValueKind.Array) continue;

                foreach (var ring in poly.EnumerateArray())
                {
                    var pts = ExtractRing(ring);
                    var area = Math.Abs(SignedArea(pts));
                    if (area > bestArea)
                    {
                        bestArea = area;
                        best = pts;
                    }
                }
            }

            return best;
        }

        private static List<(double Lon, double Lat)> ExtractRing(JsonElement ring)
        {
            var pts = new List<(double Lon, double Lat)>();

            if (ring.ValueKind != JsonValueKind.Array)
                return pts;

            foreach (var p in ring.EnumerateArray())
            {
                if (p.ValueKind != JsonValueKind.Array) continue;
                if (p.GetArrayLength() < 2) continue;

                var lon = p[0].GetDouble();
                var lat = p[1].GetDouble();
                pts.Add((lon, lat));
            }

            return pts;
        }

        private static double SignedArea(List<(double Lon, double Lat)> ring)
        {
            if (ring.Count < 3) return 0;
            double a = 0;

            for (int i = 0; i < ring.Count; i++)
            {
                var (x1, y1) = ring[i];
                var (x2, y2) = ring[(i + 1) % ring.Count];
                a += (x1 * y2) - (x2 * y1);
            }

            return a / 2.0;
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

            if (double.TryParse(latText, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
                double.TryParse(lonText, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
            {
                FollowToggle.IsChecked = true;
                await UpdateMapLocationAsync(lat, lon, DefaultZoom, addTrail: true, forceCenter: true);
            }

            await UpdateAlertPolygonsFromFilteredViewAsync();
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
                await MapView.CoreWebView2.ExecuteScriptAsync($"setView({latStr}, {lonStr}, {zoom});");
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

        private async void ClearRoute_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_mapReady || MapView?.CoreWebView2 == null) return;
            await MapView.CoreWebView2.ExecuteScriptAsync("clearRoute();");
        }

        private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
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
            MapView.CoreWebView2.PostWebMessageAsString(geoJsonFeatureCollection);
        }

        private async Task UpdateAlertPolygonsFromFilteredViewAsync()
        {
            if (!_mapReady) return;
            _vm ??= DataContext as DashboardViewModel;
            if (_vm?.AlertsView == null) return;

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
                    merged,  // ✅ combined geometry for this alert
                    null
                ));
            }

            var geojson = BuildGeoJsonFeatureCollection(expanded);
            await SetAlertPolygonsAsync(geojson);
        }

        private static string? BuildMergedMultiPolygonGeometry(IReadOnlyList<string> geometries)
        {
            try
            {
                var polygonCoordElements = new List<JsonElement>();
                var docs = new List<JsonDocument>();

                try
                {
                    foreach (var g in geometries)
                    {
                        var d = JsonDocument.Parse(g);
                        docs.Add(d);

                        var root = d.RootElement;
                        if (!root.TryGetProperty("type", out var t)) continue;

                        var type = t.GetString();

                        if (string.Equals(type, "Polygon", StringComparison.OrdinalIgnoreCase))
                        {
                            polygonCoordElements.Add(root.GetProperty("coordinates"));
                        }
                        else if (string.Equals(type, "MultiPolygon", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (var poly in root.GetProperty("coordinates").EnumerateArray())
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

                    foreach (var polyCoords in polygonCoordElements)
                        polyCoords.WriteTo(w);

                    w.WriteEndArray();
                    w.WriteEndObject();
                    w.Flush();

                    return Encoding.UTF8.GetString(ms.ToArray());
                }
                finally
                {
                    foreach (var d in docs) d.Dispose();
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

            foreach (var a in alerts)
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
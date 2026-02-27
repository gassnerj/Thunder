using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GeoJsonWeather;
using GeoJsonWeather.Models;
using GPSData;
using vMixLib;
using GeoCode = GPSData.GeoCode;
using LiveCharts;
using LiveCharts.Wpf;
using MeteorologyCore;
using ThunderApp.Models;
using ThunderApp.Services;
using ThunderApp.ViewModels;
using ThunderApp.Views;

namespace ThunderApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _didInitialMapCenter = false;
        private CancellationTokenSource? _appCts;
        private ThunderApp.ViewModels.DashboardViewModel? _dashboardVm;
        private AlertFiltersWindow? _alertFiltersWindow;
        private NwsAlertsService? _alertsService;
        private DiskLogService? _logService;
        
        private GPS GPSLocation { get; set; } = null!;
        private GeoCode GeoCode { get; set; } = null!;
        private NMEA NMEA { get; set; } = null!;
        private IGeoCodeLocation _geoCodeLocation = null!;
        private Vmix _vmixLocation = null!;
        private VmixDataApiHost? _vmixApiHost;
        private ILocationOverlayService? _locationOverlayService;
        private LocationOverlaySnapshot? _lastLocationOverlay;
        private AppSettings _appSettings = new();
        private string SerialPortName { get; set; } = "COM5";

        private SeriesCollection _seriesCollection = null!;
        // Weather station streaming (nearest station to current location).
        private CancellationTokenSource? _wxStreamCts;
        private Task? _wxStreamTask;
        private ThunderApp.Models.GeoPoint? _wxStreamAnchor;

        private readonly JsonSettingsService<UnitSettings> _unitSettingsStore = new("unitSettings.json");
        private UnitSettings _unitSettings = new();
        private int _wxConsecutiveFailures;
        private DateTime _wxBackoffUntilUtc = DateTime.MinValue;
        private StationObservationSnapshot? _lastStationSnapshot;

        // Keep chart history in base units (C, C, mph) so unit switches can re-render instantly.
        private readonly List<double> _tempHistoryC = new();
        private readonly List<double> _dewHistoryC = new();
        private readonly List<double> _windHistoryMph = new();



        public static readonly RoutedUICommand OpenUnitSettingsCommand = new(
            "Units and Theme",
            nameof(OpenUnitSettingsCommand),
            typeof(MainWindow),
            new InputGestureCollection { new KeyGesture(Key.U, ModifierKeys.Control) });

        private static TimeSpan ComputeWxBackoff(int failures, bool isForbidden)
        {
            int exp = Math.Min(6, Math.Max(0, failures - 1));
            int seconds = (int)Math.Pow(2, exp); // 1,2,4,8,16,32,64
            if (isForbidden)
                seconds = Math.Max(15, seconds);

            return TimeSpan.FromSeconds(Math.Min(120, seconds));
        }

        private void ResetWxBackoff()
        {
            _wxConsecutiveFailures = 0;
            _wxBackoffUntilUtc = DateTime.MinValue;
        }

        public MainWindow()
        {
            InitializeComponent();
            InitializeChart();
            Title = "GPS Monitor";
            InitializeGPS();
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
            CommandBindings.Add(new CommandBinding(OpenUnitSettingsCommand, OpenUnitSettings_Executed));
        }
        
        
        private void InitializeChart()
        {
            _seriesCollection = new SeriesCollection()
            {
                new LineSeries
                {
                    Title  = "Temperature",
                    Values = new ChartValues<double>(), // Data for the series
                },
                new LineSeries
                {
                    Title = "Dew Point",
                    Values = new ChartValues<double>()
                },
                new LineSeries
                {
                    Title = "Wind Speed",
                    Values = new ChartValues<double>()
                }
            };

            DataChart.Series         = _seriesCollection;
            DataChart.LegendLocation = LegendLocation.Right;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _appCts = new CancellationTokenSource();
            _unitSettings = _unitSettingsStore.Load() ?? new UnitSettings();

            // Core app services.
            var alerts = new NwsAlertsService("ThunderApp");
            var gps = new GpsService();
            var log = new DiskLogService();
            _logService = log;
            GeoJsonWeather.Api.WebData.Logger = msg => log.Log(msg);
            log.Info("MainWindow: logging initialized");
            var settings = new JsonSettingsService<AlertFilterSettings>("alertFilters.json");

            // Shared HTTP + cache for NWS/SPC helpers.
            var http = new HttpClient();
            _appSettings = AppSettingsLoader.Load();
            _locationOverlayService = BuildLocationOverlayService(http, log);
            var cacheRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");
            var cache = new SimpleDiskCache(cacheRoot);

            // Zone geometry resolver (used for range filtering when alerts only provide affected zone URLs)
            var zones = new NwsZoneGeometryService(http, cache);

            // SPC text products.
            var spcText = new SpcOutlookTextService(http, cache);

            // Load the official NWS hazard color palette (best-effort; app runs fine if it fails).
            var hazardColors = new HazardColorService(http, Path.Combine(cacheRoot, "hazardColors.txt"));
            _ = Task.Run(async () =>
            {
                try
                {
                    var pal = await hazardColors.LoadOfficialAsync(_appCts.Token);
                    HazardColorPalette.SetOfficial(pal);
                }
                catch
                {
                    // swallow - palette will fall back to built-in defaults
                }
            });

            _dashboardVm = new DashboardViewModel(alerts, gps, log, settings, zones, spcText);
            Dashboard.DataContext = _dashboardVm;
            _ = Dashboard.SetUnitSettingsAsync(_unitSettings);

            StartVmixDataApi();

            _ = InitializeAsync(_appCts.Token);
        }


        private ILocationOverlayService BuildLocationOverlayService(HttpClient http, DiskLogService log)
        {
            string token = string.IsNullOrWhiteSpace(_unitSettings.MapboxAccessToken)
                ? _appSettings.Mapbox.AccessToken
                : _unitSettings.MapboxAccessToken;

            return new ReverseGeocodeCoordinator(
                http,
                token,
                _appSettings.Nominatim.BaseUrl,
                _appSettings.Nominatim.UserAgent,
                msg => log.Log(msg));
        }

        private void StartVmixDataApi()
        {
            string prefix = Environment.GetEnvironmentVariable("THUNDER_VMIX_API_PREFIX") ?? "http://127.0.0.1:8787/";
            try
            {
                _vmixApiHost?.Stop();
                _vmixApiHost = new VmixDataApiHost(prefix, BuildVmixApiSnapshot);
                _vmixApiHost.Start();
                DiskLogService.Current?.Log($"vMix API started at {_vmixApiHost.Prefix}");
            }
            catch (Exception ex)
            {
                DiskLogService.Current?.LogException("vMix API start", ex);
            }
        }

        private VmixApiSnapshot BuildVmixApiSnapshot()
        {
            var snapshot = new VmixApiSnapshot
            {
                generatedAtUtc = DateTime.UtcNow,
                sourceRequested = _unitSettings.ObservationSource.ToString(),
                sourceActive = _unitSettings.ObservationSource.ToString(),
                vehicleStationAvailable = false,
            };

            try
            {
                Dispatcher.Invoke(() =>
                {
                    if (_dashboardVm?.CurrentLocation is GeoPoint center)
                    {
                        snapshot.centerLat = center.Lat;
                        snapshot.centerLon = center.Lon;
                    }

                    if (_dashboardVm?.FilterSettings is AlertFilterSettings fs)
                    {
                        snapshot.useRadiusFilter = fs.UseRadiusFilter;
                        snapshot.radiusMiles = fs.RadiusMiles;
                    }

                    if (_dashboardVm?.AlertsView is ICollectionView view)
                    {
                        var warnings = new List<VmixWarningDto>();
                        foreach (var o in view)
                        {
                            if (o is not NwsAlert a) continue;
                            if (string.IsNullOrWhiteSpace(a.Event) || !a.Event.Contains("Warning", StringComparison.OrdinalIgnoreCase))
                                continue;

                            warnings.Add(new VmixWarningDto
                            {
                                Id = a.Id,
                                Event = a.Event,
                                Headline = a.Headline,
                                Severity = a.Severity,
                                Urgency = a.Urgency,
                                AreaDescription = a.AreaDescription,
                                Effective = a.Effective,
                                Expires = a.Expires,
                                Ends = a.Ends,
                                Onset = a.Onset,
                            });
                        }

                        snapshot.warnings = warnings;
                    }
                });
            }
            catch { }

            var latest = _lastStationSnapshot;
            if (_unitSettings.ObservationSource == WeatherObservationSource.VehicleLocal)
            {
                snapshot.sourceActive = "VehicleLocalUnavailable";
                snapshot.observation = null;
                snapshot.rows = BuildSnapshotRows(snapshot);
                snapshot.locationRows = BuildLocationRows(snapshot);
                return snapshot;
            }

            var obs = latest?.Observation;
            if (obs is null)
            {
                snapshot.observation = null;
                snapshot.rows = BuildSnapshotRows(snapshot);
                snapshot.locationRows = BuildLocationRows(snapshot);
                return snapshot;
            }

            snapshot.observation = new VmixObservationDto
            {
                StationId = latest?.ActiveStation?.StationIdentifier ?? "",
                StationName = latest?.ActiveStation?.Name ?? "",
                TimeZone = latest?.ActiveStation?.TimeZone ?? "",
                TimestampUtc = obs.Timestamp.ToUniversalTime(),
                TemperatureF = obs.Temperature?.ToFahrenheit().Value,
                DewPointF = obs.DewPoint?.ToFahrenheit().Value,
                RelativeHumidity = obs.RelativeHumidity,
                WindMph = obs.Wind?.Speed,
                WindDirection = obs.Wind?.Direction?.ToString() ?? "",
                HeatIndexF = obs.HeatIndex?.ToFahrenheit().Value,
                WindChillF = obs.WindChill?.ToFahrenheit().Value,
                BarometricPressureInHg = obs.BarometricPressure is not null ? obs.BarometricPressure.Value / 3386.389 : null,
                SeaLevelPressureInHg = obs.SeaLevelPressure is not null ? obs.SeaLevelPressure.Value / 3386.389 : null,
            };

            snapshot.rows = BuildSnapshotRows(snapshot);
            snapshot.locationRows = BuildLocationRows(snapshot);

            return snapshot;
        }


        private IReadOnlyList<VmixLocationRowDto> BuildLocationRows(VmixApiSnapshot snapshot)
        {
            var loc = _lastLocationOverlay;
            var lat = snapshot.centerLat ?? 0;
            var lon = snapshot.centerLon ?? 0;
            return new[]
            {
                new VmixLocationRowDto
                {
                    generatedAtUtc = (loc?.GeneratedAtUtc ?? DateTime.UtcNow),
                    locLine = loc?.LocLine ?? $"{lat:0.0000}, {lon:0.0000}",
                    locDetail = loc?.LocDetail ?? $"{lat:0.0000}, {lon:0.0000}",
                    road = loc?.Road ?? "",
                    city = loc?.City ?? "",
                    state = loc?.State ?? "",
                    distMi = loc?.DistMi ?? 0.1,
                    dir = loc?.Dir ?? "N",
                    lat = loc?.Lat ?? lat,
                    lon = loc?.Lon ?? lon,
                    source = loc?.Source ?? "GPS"
                }
            };
        }

        private static IReadOnlyList<VmixSnapshotRowDto> BuildSnapshotRows(VmixApiSnapshot snapshot)
        {
            var obs = snapshot.observation;
            return new[]
            {
                new VmixSnapshotRowDto
                {
                    GeneratedAtUtc = snapshot.generatedAtUtc,
                    SourceRequested = snapshot.sourceRequested,
                    SourceActive = snapshot.sourceActive,
                    UseRadiusFilter = snapshot.useRadiusFilter,
                    RadiusMiles = snapshot.radiusMiles,
                    CenterLat = snapshot.centerLat,
                    CenterLon = snapshot.centerLon,
                    WarningCount = snapshot.warnings?.Count ?? 0,
                    StationId = obs?.StationId ?? "",
                    StationName = obs?.StationName ?? "",
                    TimeZone = obs?.TimeZone ?? "",
                    ObservationTimestampUtc = obs?.TimestampUtc,
                    TemperatureF = obs?.TemperatureF,
                    DewPointF = obs?.DewPointF,
                    RelativeHumidity = obs?.RelativeHumidity,
                    WindMph = obs?.WindMph,
                    WindDirection = obs?.WindDirection ?? "",
                    HeatIndexF = obs?.HeatIndexF,
                    WindChillF = obs?.WindChillF,
                    BarometricPressureInHg = obs?.BarometricPressureInHg,
                    SeaLevelPressureInHg = obs?.SeaLevelPressureInHg,
                }
            };
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            try { _appCts?.Cancel(); } catch { }
            try { _vmixApiHost?.Stop(); } catch { }
        }

        private async Task InitializeAsync(CancellationToken ct)
        {
            var  vmixFactory = new VmixFactory();
            Vmix vmix        = vmixFactory.CreateInstance("3fcd6206-79ff-4d05-a4fc-3a9ac7fd9752", "Message.Text");
            _vmixLocation = vmixFactory.CreateInstance("37b08594-3866-40c2-8fe4-064391bf0509", "Description.Text");

            var gpsTimer    = new PeriodicTimer(TimeSpan.FromMinutes(1));
            var screenTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));

            // Weather station updates run as a continuous stream and restart when your location moves.
            _ = Task.Run(() => RunNearestWeatherStationStreamAsync(ct), ct);
            
            Task screenLoop = Task.Run(async () =>
            {
                while (await screenTimer.WaitForNextTickAsync(ct))
                {
                    vmix.Value = DateTime.Now.ToString(CultureInfo.CurrentCulture);
                    string url = vmix.UpdateUrl();
                    await Vmix.SendRequest(url);
                }
            }, ct);

            Task gpsLoop = Task.Run(async () =>
            {
                while (await gpsTimer.WaitForNextTickAsync(ct))
                    if (NMEA.Speed > -1)
                        await UpdateLocation(_vmixLocation);
            }, ct);

            await Task.CompletedTask; // loops run until ct is cancelled
        }

        private ThunderApp.Models.GeoPoint? GetBestCurrentLocation()
        {
            // Prefer the app's unified location (GPS or manual).
            if (_dashboardVm?.CurrentLocation is ThunderApp.Models.GeoPoint p)
                return p;

            // If the VM exists but hasn't published CurrentLocation yet, fall back to its manual center.
            // This fixes the "all blank" weather station at startup when no GPS puck is connected.
            if (_dashboardVm?.FilterSettings is not null)
            {
                double lat = _dashboardVm.FilterSettings.ManualLat;
                double lon = _dashboardVm.FilterSettings.ManualLon;
                if (System.Math.Abs(lat) > 0.0001 && System.Math.Abs(lon) > 0.0001)
                    return new ThunderApp.Models.GeoPoint(lat, lon);
            }

            // Fallback to last GPS fix if VM isn't ready yet.
            if (NMEA is not null && NMEA.Status == "Valid Fix")
                return new ThunderApp.Models.GeoPoint(NMEA.Latitude, NMEA.Longitude);

            return null;
        }

        private static double KmToMiles(double km) => km * 0.621371;

        private static double DistanceMiles(ThunderApp.Models.GeoPoint a, ThunderApp.Models.GeoPoint b)
        {
            double km = GeoJsonWeather.GeoHelper.CalculateHaversineDistance(a.Lat, a.Lon, b.Lat, b.Lon);
            return KmToMiles(km);
        }

        private async Task RunNearestWeatherStationStreamAsync(CancellationToken appCt)
        {
            // Restart the observation stream if you move this far.
            const double restartMiles = 10;

            DiskLogService.Current?.Log("WX: stream supervisor started");
            while (!appCt.IsCancellationRequested)
            {
                try
                {
                    var loc = GetBestCurrentLocation();
                    if (loc is null)
                    {
                        DiskLogService.Current?.Log("WX: no location yet (GPS/manual)");
                        await Task.Delay(500, appCt);
                        continue;
                    }

                    bool needRestart = _wxStreamTask == null || _wxStreamTask.IsCompleted;
                    if (!needRestart && _wxStreamAnchor is not null)
                    {
                        var moved = DistanceMiles(_wxStreamAnchor.Value, loc.Value);
                        if (moved >= restartMiles)
                            needRestart = true;
                    }

                    if (needRestart)
                    {
                        var nowUtc = DateTime.UtcNow;
                        if (nowUtc < _wxBackoffUntilUtc)
                        {
                            var remaining = (_wxBackoffUntilUtc - nowUtc).TotalSeconds;
                            DiskLogService.Current?.Log($"WX: backoff active, delaying restart for {Math.Ceiling(remaining)}s");
                        }
                        else
                        {
                            try { _wxStreamCts?.Cancel(); } catch { }
                            _wxStreamCts?.Dispose();

                            _wxStreamCts = CancellationTokenSource.CreateLinkedTokenSource(appCt);
                            _wxStreamAnchor = loc;

                            _wxStreamTask = Task.Run(() => ConsumeObservationStreamAsync(loc.Value, _wxStreamCts.Token), _wxStreamCts.Token);
                        }
                    }
                }
                catch (Exception ex)
                {
                    DiskLogService.Current?.Log("WX: supervisor loop error");
                    DiskLogService.Current?.LogException("WX supervisor", ex);
                }

                try { await Task.Delay(1000, appCt); } catch { }
            }
        }

        private bool _wxLoggedFirstObs;

        private async Task ConsumeObservationStreamAsync(ThunderApp.Models.GeoPoint loc, CancellationToken ct)
        {
            try
            {
                DiskLogService.Current?.Log($"WX: ConsumeObservationStreamAsync start loc={loc.Lat:0.0000},{loc.Lon:0.0000}");
                await foreach (var snapshot in ObservationManager.GetNearestObservations(loc.Lat, loc.Lon, ct))
                {
                    if (ct.IsCancellationRequested) break;
                    var model = snapshot.Observation;
                    if (model is null) continue;
                    _lastStationSnapshot = snapshot;

                    if (!_wxLoggedFirstObs)
                    {
                        _wxLoggedFirstObs = true;
                        DiskLogService.Current?.Log($"WX: first obs ts={model.Timestamp:o} tempC={model.Temperature.Value} dpC={model.DewPoint.Value}");
                    }

                    if (_wxConsecutiveFailures > 0)
                        DiskLogService.Current?.Log("WX: stream recovered; clearing backoff state");
                    ResetWxBackoff();

                    if (snapshot.ActiveStation?.StationIdentifier is string sid)
                        DiskLogService.Current?.Log($"WX active station id={sid} name={snapshot.ActiveStation.Name}");

                    _ = Dashboard?.SetWeatherStationsOnMapAsync(snapshot.Stations, snapshot.ActiveStation, model, snapshot.StationObservations);

                    // Never block the UI thread: BeginInvoke instead of Invoke.
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            AirTemperature.Text = FormatTemperature(model.Temperature);
                            DewPoint.Text = FormatTemperature(model.DewPoint);

                            // Wind can be missing in some obs payloads.
                            Wind.Text = FormatWind(model.Wind);

                            LastUpdate.Text = model.Timestamp.ToLocalTime().ToString(CultureInfo.CurrentCulture);

                            ActiveStation.Text = snapshot.ActiveStation?.StationIdentifier ?? "--";
                            StationName.Text = snapshot.ActiveStation?.Name ?? "--";
                            StationTz.Text = snapshot.ActiveStation?.TimeZone ?? "--";
                            RelativeHumidity.Text = $"{Math.Round(model.RelativeHumidity)}%";
                            HeatIndex.Text = FormatTemperature(model.HeatIndex);
                            WindChill.Text = FormatTemperature(model.WindChill);
                            BarometricPressure.Text = FormatPressure(model.BarometricPressure);
                            SeaLevelPressure.Text = FormatPressure(model.SeaLevelPressure);

                            if (model.Temperature?.Value is double tC)
                                _tempHistoryC.Add(tC);
                            if (model.DewPoint?.Value is double dC)
                                _dewHistoryC.Add(dC);
                            if (model.Wind?.Speed is double wMph)
                                _windHistoryMph.Add(wMph);

                            RefreshChartSeries();
                        }
                        catch
                        {
                            // ignore UI update issues
                        }
                    }), DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested)
                {
                    _wxConsecutiveFailures++;
                    bool isForbidden = ex is HttpRequestException hre && hre.Message.Contains("HTTP 403", StringComparison.OrdinalIgnoreCase);
                    var delay = ComputeWxBackoff(_wxConsecutiveFailures, isForbidden);
                    _wxBackoffUntilUtc = DateTime.UtcNow.Add(delay);
                    DiskLogService.Current?.Log($"WX: stream failure {_wxConsecutiveFailures}; backoff {delay.TotalSeconds:0}s (forbidden={isForbidden})");
                }

                // If the stream dies early (points/zone/station fetch), surface it.
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        AirTemperature.Text = "--";
                        DewPoint.Text = "--";
                        Wind.Text = "--";
                        LastUpdate.Text = "WX error";
                        ActiveStation.Text = "--";
                        StationName.Text = "--";
                        StationTz.Text = "--";
                        RelativeHumidity.Text = "--";
                        HeatIndex.Text = "--";
                        WindChill.Text = "--";
                        BarometricPressure.Text = "--";
                        SeaLevelPressure.Text = "--";
                    }
                    catch { }
                }));

                DiskLogService.Current?.LogException("WX stream", ex);
            }
        }


        private string FormatTemperature(MeteorologyCore.ITemperature? t)
        {
            if (ConvertTemperatureValue(t) is not double value) return "--";
            return _unitSettings.TemperatureUnit == TemperatureUnit.Celsius
                ? $"{value:0.0} °C"
                : $"{value:0.0} °F";
        }

        private double? ConvertTemperatureValue(MeteorologyCore.ITemperature? t)
        {
            if (t?.Value is not double c) return null;
            return _unitSettings.TemperatureUnit == TemperatureUnit.Celsius ? c : t.ToFahrenheit().Value;
        }

        private string FormatWind(MeteorologyCore.Wind? wind)
        {
            if (wind is null) return "--";
            var val = ConvertWindValue(wind.Speed);
            if (val is null) return "--";
            var unit = _unitSettings.WindSpeedUnit switch
            {
                WindSpeedUnit.Kts => "kt",
                WindSpeedUnit.Mps => "m/s",
                _ => "mph"
            };

            return $"{Math.Round(val.Value)} {unit} ({wind.Direction})";
        }

        private double? ConvertWindValue(double? mph)
        {
            if (mph is null) return null;
            return _unitSettings.WindSpeedUnit switch
            {
                WindSpeedUnit.Kts => mph.Value * 0.868976,
                WindSpeedUnit.Mps => mph.Value * 0.44704,
                _ => mph.Value
            };
        }

        private string FormatPressure(MeteorologyCore.Pressure? p)
        {
            if (p is null) return "--";
            double pa = p.Value;
            return _unitSettings.PressureUnit switch
            {
                PressureUnit.HPa => $"{(pa / 100.0):0.00} hPa",
                PressureUnit.Pa => $"{pa:0.0} Pa",
                _ => $"{(pa / 3386.389):0.00} inHg"
            };
        }

        private void RefreshChartSeries()
        {
            _seriesCollection[0].Values = new ChartValues<double>(_tempHistoryC.Select(v =>
                _unitSettings.TemperatureUnit == TemperatureUnit.Celsius ? v : (v * 9.0 / 5.0) + 32.0));
            _seriesCollection[1].Values = new ChartValues<double>(_dewHistoryC.Select(v =>
                _unitSettings.TemperatureUnit == TemperatureUnit.Celsius ? v : (v * 9.0 / 5.0) + 32.0));
            _seriesCollection[2].Values = new ChartValues<double>(_windHistoryMph.Select(v => ConvertWindValue(v) ?? v));
        }

        private async Task RefreshLatestWeatherPresentationAsync()
        {
            var snap = _lastStationSnapshot;
            var model = snap?.Observation;
            if (snap is null || model is null) return;

            AirTemperature.Text = FormatTemperature(model.Temperature);
            DewPoint.Text = FormatTemperature(model.DewPoint);
            Wind.Text = FormatWind(model.Wind);
            LastUpdate.Text = model.Timestamp.ToLocalTime().ToString(CultureInfo.CurrentCulture);

            ActiveStation.Text = snap.ActiveStation?.StationIdentifier ?? "--";
            StationName.Text = snap.ActiveStation?.Name ?? "--";
            StationTz.Text = snap.ActiveStation?.TimeZone ?? "--";
            RelativeHumidity.Text = $"{Math.Round(model.RelativeHumidity)}%";
            HeatIndex.Text = FormatTemperature(model.HeatIndex);
            WindChill.Text = FormatTemperature(model.WindChill);
            BarometricPressure.Text = FormatPressure(model.BarometricPressure);
            SeaLevelPressure.Text = FormatPressure(model.SeaLevelPressure);

            RefreshChartSeries();
            await Dashboard.SetWeatherStationsOnMapAsync(snap.Stations, snap.ActiveStation, model, snap.StationObservations);
        }

        private async Task ApplyUnitSettingsAsync()
        {
            _unitSettingsStore.Save(_unitSettings);

            // Rebuild location service in case token/user geocode settings changed.
            if (_logService is not null)
                _locationOverlayService = BuildLocationOverlayService(new HttpClient(), _logService);

            await Dashboard.SetUnitSettingsAsync(_unitSettings);
            await RefreshLatestWeatherPresentationAsync();
        }

        private async void OpenUnitSettings_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var win = new UnitSettingsWindow(_unitSettings) { Owner = this };
            if (win.ShowDialog() != true) return;

            _unitSettings = win.Result;
            await ApplyUnitSettingsAsync();
        }

        private void InitializeGPS()
        {
            StatusBarItem.Text = "Not Connected";
            GPSLocation = new GPS(SerialPortName);
            GPSLocation.MessageReceived += GPSLocation_MessageReceived;

            GeoCode = new GeoCode("AIzaSyC_opo2I9Fs8dYeVe_FPHTWlNCYpzNX3F4");
        }

        private async void GPSLocation_MessageReceived(object? sender, GPSMessageEventArgs e)
        {
            try
            {
                NMEA = e.NMEASentence;

                if (NMEA.Status != "Valid Fix")
                    return;

                // Update UI elements based on GPS data
                await UpdateUIWithGPSData();
            }
            catch (Exception ex)
            {
                throw; // TODO handle exception
            }
        }

        private async Task UpdateUIWithGPSData()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                StatusBarItem.Text = NMEA.Status;
                Coordinates.Text   = $"{NMEA.Latitude}, {NMEA.Longitude}";
                Speed.Text         = $"{Math.Round(NMEA.Speed)} MPH";
                Altimeter.Text     = $"{Math.Round(NMEA.Altitude)} ft";
            });

            if (Dashboard == null) return;

            if (!_didInitialMapCenter)
            {
                _didInitialMapCenter = true;
                await Dashboard.UpdateMapLocationAsync(NMEA.Latitude, NMEA.Longitude, zoom: 12, addTrail: true, forceCenter: true);
            }
            else
            {
                await Dashboard.UpdateMapLocationAsync(NMEA.Latitude, NMEA.Longitude, zoom: 12, addTrail: true, forceCenter: false);
            }

            // Feed the VM the latest GPS location; the VM owns refresh scheduling.
            _dashboardVm?.SetCurrentLocation(NMEA.Latitude, NMEA.Longitude);
            _ = RefreshLocationOverlayAsync(new GeoPoint(NMEA.Latitude, NMEA.Longitude));
        }

        private async Task UpdateLocation(Vmix vmix)
        {
            if (NMEA.Status != "Valid Fix")
                return;

            var line = _lastLocationOverlay?.LocLine ?? $"{NMEA.Latitude:0.0000}, {NMEA.Longitude:0.0000}";
            vmix.Value = line;
            string locationUrl = vmix.UpdateUrl();
            await Vmix.SendRequest(locationUrl);

            await Dispatcher.InvokeAsync(() =>
            {
                if (_lastLocationOverlay is null) return;
                if (!string.IsNullOrWhiteSpace(_lastLocationOverlay.Road)) Road.Text = _lastLocationOverlay.Road;
                if (!string.IsNullOrWhiteSpace(_lastLocationOverlay.City)) City.Text = _lastLocationOverlay.City;
                if (!string.IsNullOrWhiteSpace(_lastLocationOverlay.State)) State.Text = _lastLocationOverlay.State;
            });
        }
        private async void StartGPS_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                GPSLocation.Read();

                await Task.Delay(1000);

                await UpdateLocation(_vmixLocation);

                StartGPS.IsEnabled                     = false;
                //serialPortsToolStripMenuItem.Enabled = false;
            }
            catch (Exception ex)
            {
                LogTextBox.AppendText(ex.Message);
                LogTextBox.AppendText(Environment.NewLine);
                StartGPS.IsEnabled                     = true;
                //serialPortsToolStripMenuItem.Enabled = true;
            }
        }

        private void StopGPS_OnClick(object sender, RoutedEventArgs e)
        {
            GPSLocation.StopReader();
            StartGPS.IsEnabled                     = true;
            //serialPortsToolStripMenuItem.Enabled = true;
        }
        
        private void MenuItem_New_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MenuItem_Save_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MenuItem_Cut_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MenuItem_Copy_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MenuItem_Paste_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MenuItem_About_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private async Task RefreshLocationOverlayAsync(GeoPoint gps)
        {
            if (_locationOverlayService is null) return;
            try
            {
                _lastLocationOverlay = await _locationOverlayService.GetSnapshotAsync(gps, _appCts?.Token ?? CancellationToken.None);

                await Dispatcher.InvokeAsync(() =>
                {
                    if (_lastLocationOverlay is null) return;
                    if (!string.IsNullOrWhiteSpace(_lastLocationOverlay.Road))
                        Road.Text = _lastLocationOverlay.Road;
                    if (!string.IsNullOrWhiteSpace(_lastLocationOverlay.City))
                        City.Text = _lastLocationOverlay.City;
                    if (!string.IsNullOrWhiteSpace(_lastLocationOverlay.State))
                        State.Text = _lastLocationOverlay.State;
                });
            }
            catch { }
        }

        private void OpenAlertFilters_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_dashboardVm == null)
                return;

            // Single instance: if it's already open, just bring it forward.
            if (_alertFiltersWindow != null)
            {
                if (_alertFiltersWindow.IsVisible)
                {
                    _alertFiltersWindow.Activate();
                    return;
                }

                _alertFiltersWindow = null;
            }

            var w = new AlertFiltersWindow
            {
                Owner = this,
                DataContext = _dashboardVm
            };

            w.Closed += (_, _) => _alertFiltersWindow = null;
            _alertFiltersWindow = w;
            w.Show();
        }

        private void OpenMapStyling_OnClick(object sender, RoutedEventArgs e)
        {
            if (_dashboardVm?.FilterSettings == null) return;

            var win = new MapStylingWindow(_dashboardVm.FilterSettings)
            {
                Owner = this
            };

            if (win.ShowDialog() == true)
            {
                // Persist
                try { _dashboardVm.SaveFiltersCommand.Execute(null); } catch { }
            }
        }

        private void OpenMapStyling_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            OpenMapStyling_OnClick(sender, e);
        }

        // --- Custom title bar ---
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IsClickOnCaptionButton(e.OriginalSource as DependencyObject)) return;

            if (e.ClickCount == 2)
            {
                ToggleMaximize();
                return;
            }

            try { DragMove(); } catch { }
        }

        private static bool IsClickOnCaptionButton(DependencyObject? original)
        {
            if (original is null) return false;
            var cur = original;
            while (cur is not null)
            {
                if (cur is System.Windows.Controls.Primitives.ButtonBase) return true;
                cur = System.Windows.Media.VisualTreeHelper.GetParent(cur);
            }
            return false;
        }

        private void Minimize_OnClick(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);
        private void Maximize_OnClick(object sender, RoutedEventArgs e) => ToggleMaximize();
        private void Close_OnClick(object sender, RoutedEventArgs e) => Close();

        private void ToggleMaximize()
        {
            if (WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(this);
            else SystemCommands.MaximizeWindow(this);
        }

    }
}
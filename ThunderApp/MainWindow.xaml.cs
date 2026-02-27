using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
        
        private GPS GPSLocation { get; set; } = null!;
        private GeoCode GeoCode { get; set; } = null!;
        private NMEA NMEA { get; set; } = null!;
        private IGeoCodeLocation _geoCodeLocation = null!;
        private Vmix _vmixLocation = null!;
        private string SerialPortName { get; set; } = "COM5";

        private SeriesCollection _seriesCollection = null!;
        private List<double> temperatureData = null!;
        private List<double> dewPointData = null!;
        private List<double> windData = null!;

        // Weather station streaming (nearest station to current location).
        private CancellationTokenSource? _wxStreamCts;
        private Task? _wxStreamTask;
        private ThunderApp.Models.GeoPoint? _wxStreamAnchor;
        private int _wxConsecutiveFailures;
        private DateTime _wxBackoffUntilUtc = DateTime.MinValue;


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

        private enum PressureDisplayUnit { InHg, HPa, Pa }
        private PressureDisplayUnit _pressureUnit = PressureDisplayUnit.InHg;

        public MainWindow()
        {
            InitializeComponent();
            InitializeChart();
            Title = "GPS Monitor";
            InitializeGPS();
            Loaded += MainWindow_Loaded;
        }
        
        
        private void InitializeChart()
        {
            temperatureData = new List<double>();
            dewPointData    = new List<double>();
            windData        = new List<double>();
            
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

            // Core app services.
            var alerts = new NwsAlertsService("ThunderApp");
            var gps = new GpsService();
            var log = new DiskLogService();
            GeoJsonWeather.Api.WebData.Logger = msg => log.Log(msg);
            log.Info("MainWindow: logging initialized");
            var settings = new JsonSettingsService<AlertFilterSettings>("alertFilters.json");

            // Shared HTTP + cache for NWS/SPC helpers.
            var http = new HttpClient();
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

            _ = InitializeAsync(_appCts.Token);
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
                            AirTemperature.Text = model.Temperature.ToFahrenheit().ToString();
                            DewPoint.Text = model.DewPoint.ToFahrenheit().ToString();

                            // Wind can be missing in some obs payloads.
                            if (model.Wind is not null)
                                Wind.Text = $"{Math.Round(model.Wind.Speed)} ({model.Wind.Direction})";
                            else
                                Wind.Text = "--";

                            LastUpdate.Text = model.Timestamp.ToLocalTime().ToString(CultureInfo.CurrentCulture);

                            ActiveStation.Text = snapshot.ActiveStation?.StationIdentifier ?? "--";
                            StationName.Text = snapshot.ActiveStation?.Name ?? "--";
                            StationTz.Text = snapshot.ActiveStation?.TimeZone ?? "--";
                            RelativeHumidity.Text = $"{Math.Round(model.RelativeHumidity)}%";
                            HeatIndex.Text = model.HeatIndex is not null ? model.HeatIndex.ToFahrenheit().ToString() : "--";
                            WindChill.Text = model.WindChill is not null ? model.WindChill.ToFahrenheit().ToString() : "--";
                            BarometricPressure.Text = FormatPressure(model.BarometricPressure);
                            SeaLevelPressure.Text = FormatPressure(model.SeaLevelPressure);

                            if (model.Temperature.ToFahrenheit().Value is double t)
                                temperatureData.Add(t);
                            if (model.DewPoint.ToFahrenheit().Value is double d)
                                dewPointData.Add(d);

                            if (model.Wind is not null)
                                windData.Add(model.Wind.Speed);

                            _seriesCollection[0].Values = new ChartValues<double>(temperatureData);
                            _seriesCollection[1].Values = new ChartValues<double>(dewPointData);
                            _seriesCollection[2].Values = new ChartValues<double>(windData);
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


        private string FormatPressure(MeteorologyCore.Pressure? p)
        {
            if (p is null) return "--";
            double pa = p.Value;
            return _pressureUnit switch
            {
                PressureDisplayUnit.HPa => $"{(pa / 100.0):0.00} hPa",
                PressureDisplayUnit.Pa => $"{pa:0.0} Pa",
                _ => $"{(pa / 3386.389):0.00} inHg"
            };
        }

        private void SetPressureUnit(PressureDisplayUnit unit)
        {
            _pressureUnit = unit;
            InHgUnitMenuItem.IsChecked = unit == PressureDisplayUnit.InHg;
            HpaUnitMenuItem.IsChecked = unit == PressureDisplayUnit.HPa;
            PaUnitMenuItem.IsChecked = unit == PressureDisplayUnit.Pa;
        }

        private void PressureUnitInHg_OnClick(object sender, RoutedEventArgs e) => SetPressureUnit(PressureDisplayUnit.InHg);
        private void PressureUnitHpa_OnClick(object sender, RoutedEventArgs e) => SetPressureUnit(PressureDisplayUnit.HPa);
        private void PressureUnitPa_OnClick(object sender, RoutedEventArgs e) => SetPressureUnit(PressureDisplayUnit.Pa);

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
        }

        private async Task UpdateLocation(Vmix vmix)
        {
            if (NMEA.Status == "Valid Fix")
            {
                await Task.Run(async () =>
                {
                    _geoCodeLocation = GeoCode.GetData(NMEA)!;

                    // Update VMix and other UI elements
                    await Dispatcher.Invoke(async () =>
                    {
                        vmix.Value = $"Direction: ({NMEA.GetCardinalDirection(NMEA.Course)}) - Location: {_geoCodeLocation!.Road} - {_geoCodeLocation.City}, {_geoCodeLocation.County}, {_geoCodeLocation.State}";
                        string locationUrl = vmix.UpdateUrl();
                        await Vmix.SendRequest(locationUrl); // Uncomment and replace with actual logic

                        Road.Text = _geoCodeLocation!.Road;
                        City.Text = _geoCodeLocation.City;
                        County.Text = _geoCodeLocation.County;
                        State.Text = _geoCodeLocation.State;
                    });
                });
            }
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
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
            var apiTimer = new PeriodicTimer(TimeSpan.FromMinutes(1));
            
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

            Task apiLoop = Task.Run(async () =>
            {
                while (await apiTimer.WaitForNextTickAsync(ct))
                {
                    await foreach (ObservationModel? model in ObservationManager.GetNearestObservations(33.9595, -98.6812, ct))
                    {
                        if (model is null)
                            break;
                        Dispatcher.Invoke(() =>
                        {
                            AirTemperature.Text = model.Temperature.ToFahrenheit().ToString();
                            DewPoint.Text       = model.Temperature.ToFahrenheit().ToString();
                            Wind.Text           = $"{Math.Round(model.Wind.Speed)} ({model.Wind.Direction})";
                            LastUpdate.Text     = DateTime.Now.ToString(CultureInfo.CurrentCulture);

                            temperatureData.Add(model.Temperature.ToFahrenheit().Value);
                            dewPointData.Add(model.Temperature.ToFahrenheit().Value);
                            windData.Add(model.Wind.Speed);
                            
                            _seriesCollection[0].Values = new ChartValues<double>(temperatureData);
                            _seriesCollection[1].Values = new ChartValues<double>(dewPointData);
                            _seriesCollection[2].Values = new ChartValues<double>(windData);
                        });
                    }
                }
            }, ct);

            await Task.CompletedTask; // loops run until ct is cancelled
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
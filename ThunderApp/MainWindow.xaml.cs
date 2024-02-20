using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GeoJsonWeather;
using GeoJsonWeather.Models;
using GPSData;
using vMixLib;
using GeoCode = GPSData.GeoCode;
using LiveCharts;
using LiveCharts.Wpf;

namespace ThunderApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private GPS GPSLocation { get; set; } = new GPS("COM5");
        private GeoCode GeoCode { get; set; } = new GeoCode("AIzaSyC_opo2I9Fs8dYeVe_FPHTWlNCYpzNX3F4");
        private NMEA NMEA { get; set; } = null!;
        private IGeoCodeLocation _geoCodeLocation = null!;
        private Vmix _vmixLocation = null!;
        private string SerialPortName { get; set; } = "COM5";

        private SeriesCollection? _seriesCollection;
        private List<double>? _temperatureData;
        private List<double>? _dewPointData;
        private List<double>? _windData;

        public MainWindow()
        {
            InitializeComponent();
            InitializeChart();
            InitializeGPS();
            Title = "GPS Monitor";
            Loaded += async (_, _) => await InitializeAsync();
        }

        private void InitializeChart()
        {
            StopGPS.IsEnabled = false;

            _temperatureData = new List<double>();
            _dewPointData = new List<double>();
            _windData = new List<double>();

            _seriesCollection = new SeriesCollection()
            {
                new LineSeries { Title = "Temperature", Values = new ChartValues<double>(_temperatureData) },
                new LineSeries { Title = "Dew Point", Values = new ChartValues<double>(_dewPointData) },
                new LineSeries { Title = "Wind Speed", Values = new ChartValues<double>(_windData) }
            };

            DataChart.Series = _seriesCollection;
            DataChart.LegendLocation = LegendLocation.Right;
        }

        private async Task InitializeAsync()
        {
            var vmixFactory = new VmixFactory();
            Vmix vmix = vmixFactory.CreateInstance("3fcd6206-79ff-4d05-a4fc-3a9ac7fd9752", "Message.Text");
            _vmixLocation = vmixFactory.CreateInstance("37b08594-3866-40c2-8fe4-064391bf0509", "Description.Text");

            var gpsTimer = new PeriodicTimer(TimeSpan.FromMinutes(1));
            var screenTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            var apiTimer = new PeriodicTimer(TimeSpan.FromMinutes(1));

            await Task.WhenAll(ScreenLoop(), GpsLoop(), ApiLoop());
            return;

            async Task GpsLoop()
            {
                while (await gpsTimer.WaitForNextTickAsync())
                {
                    if (NMEA.Speed > -1)
                        await UpdateLocation(_vmixLocation);
                }
            }

            async Task ApiLoop()
            {
                while (await apiTimer.WaitForNextTickAsync())
                {
                    await foreach (ObservationModel? model in ObservationManager.GetNearestObservations(NMEA.Latitude, NMEA.Longitude))
                    {
                        if (model is null)
                            return;

                        Dispatcher.Invoke(() =>
                        {
                            AirTemperature.Text = model.Temperature.ToFahrenheit().ToString();
                            DewPoint.Text       = model.Temperature.ToFahrenheit().ToString();
                            Wind.Text           = $"{Math.Round(model.Wind.Speed)} ({model.Wind.Direction})";
                            LastUpdate.Text     = DateTime.Now.ToString(CultureInfo.CurrentCulture);

                            _temperatureData?.Add(model.Temperature.ToFahrenheit().Value);
                            _dewPointData?.Add(model.Temperature.ToFahrenheit().Value);
                            _windData?.Add(model.Wind.Speed);

                            if (_seriesCollection == null)
                                return;
                            
                            _seriesCollection[0].Values = new ChartValues<double>(_temperatureData);
                            _seriesCollection[1].Values = new ChartValues<double>(_dewPointData);
                            _seriesCollection[2].Values = new ChartValues<double>(_windData);
                        });
                    }
                }
            }

            async Task ScreenLoop()
            {
                while (await screenTimer.WaitForNextTickAsync())
                {
                    vmix.Value = DateTime.Now.ToString(CultureInfo.CurrentCulture);
                    string url = vmix.UpdateUrl();
                    await Vmix.SendRequest(url);
                }
            }
        }

        private void InitializeGPS()
        {
            StatusBarItem.Text = "Not Connected";
            GPSLocation = new GPS(SerialPortName);
            GPSLocation.MessageReceived += GPSLocation_MessageReceived;

            GeoCode = new GeoCode("AIzaSyC_opo2I9Fs8dYeVe_FPHTWlNCYpzNX3F4");
        }

        private void GPSLocation_MessageReceived(object? sender, GPSMessageEventArgs e)
        {
            NMEA = e.NMEASentence;

            if (NMEA.Status != "Valid Fix")
                return;

            UpdateUIWithGPSData();
        }

        private void UpdateUIWithGPSData()
        {
            Dispatcher.Invoke(() =>
            {
                StatusBarItem.Text = NMEA.Status;
                Coordinates.Text = $"{NMEA.Latitude}, {NMEA.Longitude}";
                Speed.Text = $"{Math.Round(NMEA.Speed)} MPH";
                Altimeter.Text = $"{Math.Round(NMEA.Altitude)} ft";
            });
        }

        private async Task UpdateLocation(Vmix vmix)
        {
            if (NMEA.Status == "Valid Fix")
            {
                await Task.Run(async () =>
                {
                    _geoCodeLocation = GeoCode.GetData(NMEA)!;

                    await Dispatcher.Invoke(async () =>
                    {
                        vmix.Value = $"Direction: ({NMEA.GetCardinalDirection(NMEA.Course)}) - Location: {_geoCodeLocation.Road} - {_geoCodeLocation.City}, {_geoCodeLocation.County}, {_geoCodeLocation.State}";
                        string locationUrl = vmix.UpdateUrl();
                        await Vmix.SendRequest(locationUrl);

                        Road.Text = _geoCodeLocation.Road;
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
                Thread.Sleep(1000);

                await UpdateLocation(_vmixLocation);

                StartGPS.IsEnabled = false;
                StopGPS.IsEnabled = true;
                LogTextBox.Clear();
            }
            catch (Exception ex)
            {
                LogTextBox.AppendText(ex.Message);
                LogTextBox.AppendText(Environment.NewLine);
                StartGPS.IsEnabled = true;
                StopGPS.IsEnabled = false;
            }
        }

        private void StopGPS_OnClick(object sender, RoutedEventArgs e)
        {
            GPSLocation.StopReader();
            StartGPS.IsEnabled = true;
            StopGPS.IsEnabled = false;
            LogTextBox.Clear();
        }

        #region menu clicks

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

        #endregion
    }
}

using System.Globalization;
using GPSData;
using GPSMonitor2;
using vMixLib;

namespace GPSMonitor;

public partial class GPSMonitorForm : Form
{
    private GPS GPSLocation { get; set; }
    private GeoCode GeoCode { get; set; }
    private NMEA NMEA { get; set; } = null!;

    private IGeoCodeLocation _geoCodeLocation = null!;

    internal string SerialPortName { get; set; } = "COM5";
    private Vmix _vmixLocation = null!;

    public GPSMonitorForm()
    {
        InitializeComponent();
        Text                        =  @"GPS Monitor";
        gpsStatusLabel.Text         =  @"Not Connected";
        GPSLocation                 =  new GPS(SerialPortName);
        GPSLocation.MessageReceived += GPSLocation_MessageReceived;

        Load    += Form1_Load;
        GeoCode =  new GeoCode("AIzaSyC_opo2I9Fs8dYeVe_FPHTWlNCYpzNX3F4");
    }

    public override sealed string Text
    {
        get => base.Text;
        set => base.Text = value;
    }

    private async void Form1_Load(object? sender, EventArgs e)
    {
        var  vmixFactory = new VmixFactory();
        Vmix vmix        = vmixFactory.CreateInstance("3fcd6206-79ff-4d05-a4fc-3a9ac7fd9752", "Message.Text");
        _vmixLocation = vmixFactory.CreateInstance("37b08594-3866-40c2-8fe4-064391bf0509", "Description.Text");

        var gpsTimer    = new PeriodicTimer(TimeSpan.FromMinutes(1));
        var screenTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        Task screenLoop = Task.Run(async () =>
        {
            while (await screenTimer.WaitForNextTickAsync())
            {
                vmix.Value = DateTime.Now.ToString(CultureInfo.CurrentCulture);
                string url = vmix.UpdateUrl();
                await Vmix.SendRequest(url);
            }
        });

        Task gpsLoop = Task.Run(async () =>
        {
            while (await gpsTimer.WaitForNextTickAsync())
                if (NMEA.Speed > -1)
                    await UpdateLocation(_vmixLocation);
        });

        await Task.WhenAll(screenLoop, gpsLoop);
    }


    private async Task UpdateLocation(Vmix vmix)
    {
        if (NMEA.Status == "Valid Fix")
        {
            await Task.Run(async () =>
            {
                _geoCodeLocation = GeoCode.GetData(NMEA)!;

                vmix.Value = $"Direction: ({NMEA.GetCardinalDirection(NMEA.Course)}) - Location: {_geoCodeLocation!.Road} - {_geoCodeLocation.City}, {_geoCodeLocation.County}, {_geoCodeLocation.State}";
                string locationUrl = vmix.UpdateUrl();
                await Vmix.SendRequest(locationUrl);

                if (InvokeRequired)
                {
                    Invoke(() =>
                    {
                        labelRoad.Text   = _geoCodeLocation!.Road;
                        labelCity.Text   = _geoCodeLocation.City;
                        labelCounty.Text = _geoCodeLocation.County;
                        labelState.Text  = _geoCodeLocation.State;
                    });
                }
            });
        }
    }

    private async void btnStart_Click(object sender, EventArgs e)
    {
        try
        {
            GPSLocation.Read();

            Thread.Sleep(1000);

            await UpdateLocation(_vmixLocation);

            btnStart.Enabled                     = false;
            serialPortsToolStripMenuItem.Enabled = false;
        }
        catch (Exception ex)
        {
            textBoxGpsLog.AppendText(ex.Message);
            textBoxGpsLog.AppendText(Environment.NewLine);
            btnStart.Enabled                     = true;
            serialPortsToolStripMenuItem.Enabled = true;
        }
    }

    private void btnStop_Click(object sender, EventArgs e)
    {
        GPSLocation.StopReader();
        btnStart.Enabled                     = true;
        serialPortsToolStripMenuItem.Enabled = true;
    }

    private void GPSLocation_MessageReceived(object? sender, GPSMessageEventArgs e)
    {
        NMEA = e.NMEASentence;

        if (NMEA.Status != "Valid Fix")
            return;

        if (InvokeRequired)
        {
            Invoke(() =>
            {
                //textBoxGpsLog.AppendText($"{NMEA.Latitude}, {NMEA.Longitude}");
                //textBoxGpsLog.AppendText(Environment.NewLine);

                gpsStatusLabel.Text = NMEA.Status;
                labelCoords.Text    = $@"{NMEA.Latitude}, {NMEA.Longitude}";
                labelSpeed.Text     = $@"{Math.Round(NMEA.Speed)} MPH";
                labelAltimeter.Text = $@"{Math.Round(NMEA.Altitude)} ft";
                labelHeading.Text   = $@"{Math.Round(NMEA.Course)} ({NMEA.GetCardinalDirection(NMEA.Course)})";
            });
        }
        ;
    }

    private void serialPortsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using var serialPortForm = new SerialPorts(this);
        DialogResult       result         = serialPortForm.ShowDialog();
        if (result == DialogResult.OK)
        {
            GPSLocation.UpdatePortName(SerialPortName);
        }
    }

    private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
    {
        var about = new AboutBox1();
        about.ShowDialog();
    }
}
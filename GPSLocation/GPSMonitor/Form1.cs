using GPSData;
using System.Runtime.CompilerServices;

namespace GPSMonitor
{
    public partial class GPSMonitorForm : Form
    {
        private GPS GPSLocation { get; set; }
        private GeoCode GeoCode { get; set; }
        private NMEA NMEA { get; set; }

        IGeoCodeLocation geoCodeLocation;

        internal string SerialPortName { get; set; } = "COM3";

        public GPSMonitorForm()
        {
            InitializeComponent();
            gpsStatusLabel.Text = "Not Connected";
            GPSLocation = new GPS(SerialPortName);
            GPSLocation.MessageReceived += GPSLocation_MessageReceived;
            
            Load += Form1_Load;
            GeoCode = new GeoCode("AIzaSyC_opo2I9Fs8dYeVe_FPHTWlNCYpzNX3F4");
        }

        private async void Form1_Load(object? sender, EventArgs e)
        {
            var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
            while (await timer.WaitForNextTickAsync())
                UpdateLocation();
        }

        private void UpdateLocation()
        {
            if (NMEA is not null)
            {
                if (NMEA.Status == "Valid Fix")
                {
                    geoCodeLocation = GeoCode.GetData(NMEA.Latitude, NMEA.Longitude);
                    labelRoad.Text = geoCodeLocation!.Road;
                    labelCity.Text = geoCodeLocation.City;
                    labelCounty.Text = geoCodeLocation.County;
                    labelState.Text = geoCodeLocation.State;
                }
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                GPSLocation.Read();

                Thread.Sleep(3000);

                UpdateLocation();

                btnStart.Enabled = false;
                serialPortsToolStripMenuItem.Enabled = false;

            } catch (Exception ex)
            {
                textBoxGpsLog.AppendText(ex.Message);
                textBoxGpsLog.AppendText(Environment.NewLine);
                btnStart.Enabled = true;
                serialPortsToolStripMenuItem.Enabled = true;
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            GPSLocation.StopReader();
            btnStart.Enabled = true;
            serialPortsToolStripMenuItem.Enabled = true;
        }

        private void GPSLocation_MessageReceived(object? sender, GPSMessageEventArgs e)
        {
            NMEA = e.NMEASentence;

            if (NMEA.Status == "Valid Fix")
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        gpsStatusLabel.Text = NMEA.Status;
                        labelCoords.Text = $"{NMEA.Latitude}, {NMEA.Longitude}";
                        labelSpeed.Text = $"{Math.Round(NMEA.Speed)} MPH";
                        labelAltimeter.Text = $"{Math.Round(NMEA.Altitude)} ft";
                        labelHeading.Text = $"{Math.Round(NMEA.Course)} ({NMEA.GetCardinalDirection(NMEA.Course)})";
                    }));
                };
            }
        }

        private void serialPortsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var serialPortForm = new SerialPorts(this);
            var result = serialPortForm.ShowDialog();
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
}
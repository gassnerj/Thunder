using GeoJsonWeather;
using GPSData;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WeatherAlertsWF;

public partial class MainForm : Form
{
    private readonly FeatureCollection _featureCollection = new();
    private readonly IList<Alert> _alerts = new List<Alert>();
    private readonly BindingSource _source = new();
    private IEnumerable<string> _states;

    public MainForm()
    {
        InitializeComponent();
        
        Text = "Alerts";

        alertsDataGrid.RowPrePaint += AlertsDataGrid_RowPrePaint;
        alertsDataGrid.CellDoubleClick += AlertsDataGrid_CellDoubleClick;

        tornadoCheckBox.Click += TornadoCheckBox_CheckedChanged;
        severeCheckBox.Click += SevereCheckBox_CheckedChanged;
        flashFloodCheckBox.Click += FlashFloodCheckBox_CheckedChanged;
        floodCheckBox.Click += FloodCheckBox_CheckedChanged;
        blizzardCheckBox.Click += BlizzardCheckBox_CheckedChanged;
        allCheckBox.Click += AllCheckBox_CheckedChanged;
        myLocationsToolStripMenuItem.Click += MyLocationsToolStripMenuItem_Click;

        tornadoCheckBox.Checked = true;
        severeCheckBox.Checked = true;
        flashFloodCheckBox.Checked = true;
        floodCheckBox.Checked = true;
        blizzardCheckBox.Checked = true;
        
        _source.DataSource = _alerts;
        alertsDataGrid.DataSource = _source;
        alertsDataGrid.AllowUserToAddRows = false;
        alertsDataGrid.RowHeadersVisible = false;

        //_ = GetGps();
    }

    private void MyLocationsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        var locationsForm = new LocationsForm();
        DialogResult result = locationsForm.ShowDialog();
        
        if (result == DialogResult.OK)
        {
            _states = new List<string>(locationsForm.UserLocations.ToList());
        }
    }

    private void AllCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        foreach (Control control in Controls)
        {
            if (control != null && control is CheckBox box)
            {
                var checkBox = box;
                if (checkBox.Checked && checkBox.Name != "allCheckBox")
                {
                    checkBox.Checked = !checkBox.Checked;
                }
            }
        }
        FilterAlerts();
    }

    private void BlizzardCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        allCheckBox.Checked = false;
        FilterAlerts();
    }

    private void FloodCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        allCheckBox.Checked = false;
        FilterAlerts();
    }

    private void FlashFloodCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        allCheckBox.Checked = false;
        FilterAlerts();
    }

    private void SevereCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        allCheckBox.Checked = false;
        FilterAlerts();
    }

    private void TornadoCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        allCheckBox.Checked = false;
        FilterAlerts();
    }

    private void FilterAlerts()
    {
        _alerts.Clear();

        foreach (Control control in Controls)
        {
            if (control != null && control is CheckBox box)
            {
                var checkBox = box;
                if (checkBox.Checked && checkBox.Name != "allCheckBox")
                {
                    _featureCollection.Alerts
                        .Where(a => a != null)
                        .Where(a => a.Event == checkBox.Text)
                        .Cast<Alert>()
                        .OrderByDescending(t => t.Effective)
                        .ToList()
                        .ForEach(a => _alerts.Add(a));
                } else if (checkBox.Name == "allCheckBox" && checkBox.Checked)
                {
                    if (_featureCollection.Alerts.Count == 0) continue;

                    _featureCollection.Alerts
                        .Cast<Alert>()
                        .Where(t => t != null)
                        .OrderByDescending(t => t.Effective)
                        .ToList()
                        .ForEach(a => _alerts.Add(a));
                }
            }
        }
        _source.ResetBindings(false);
    }

    private void AlertsDataGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex > -1)
        {
            var alertForm = new AlertForm(alertsDataGrid.Rows[e.RowIndex].DataBoundItem as Alert);
            alertForm.Show();
        }
    }

    private void AlertsDataGrid_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
    {
        if (_featureCollection.Alerts.Count > 0)
        {
            Alert alert = alertsDataGrid.Rows[e.RowIndex].DataBoundItem as Alert;
            if (alert != null)
            {
                alertsDataGrid.Rows[e.RowIndex].DefaultCellStyle.BackColor = alert.AlertColor;
                alertsDataGrid.Rows[e.RowIndex].DefaultCellStyle.ForeColor = alert.SecondaryColor;
            }
        }
    }

    private async Task GetGps()
    {
        GPS gps = new();
        gps.MessageReceived += Gps_MessageReceived;
        await gps.Read();
    }

    private void Gps_MessageReceived(object sender, GPSMessageEventArgs e)
    {
        gpsCoords.Visible = true;
        gpsCoords.Text = e.Coordinate.ToString();
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        gpsCoords.Visible = false;
        _featureCollection.AlertMessage += FeatureCollection_AlertMessage;
        _featureCollection.AlertIssued += FeatureCollection_AlertIssued;
        _featureCollection.RefreshInterval = 1;

        string url = new NwsApiStringBuilder().GetAll();

        _ = _featureCollection.FetchData(url);

        Ticker.Tick(_featureCollection.FetchData(url), TimeSpan.FromMinutes(_featureCollection.RefreshInterval));
        Ticker.Tick(new Action(_featureCollection.PurgeAlerts), TimeSpan.FromSeconds(30));
        //Ticker.Tick(new Action(GetLastRefresh), TimeSpan.FromSeconds(2));
    }

    private void GetLastRefresh() => lastRefreshStatusLabel.Text = _featureCollection.LastRefresh.ToShortTimeString();

    private void FeatureCollection_AlertIssued(object sender, AlertIssuedEventArgs e)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(new Action(() =>
            {
                if (e.Alert is TornadoWarning || e.Alert is SevereThunderstormWarning)
                {
                    SoundPlayer soundPlayer = new(@"C:\Users\byrd8\RiderProjects\WeatherAlerts\WeatherAlertsWF\alert.wav");
                    soundPlayer.Play();

                    var alertForm = new AlertForm(e.Alert as Alert);
                    alertForm.Show();
                    alertForm.BringToFront();
                }
            }));
        }
    }

    private void FeatureCollection_AlertMessage(object sender, AlertMessageEventArgs e)
    {
        try
        {
            toolStripStatusLabel1.Text = e.Message;
            lastRefreshStatusLabel.Text = _featureCollection.LastRefresh.ToShortTimeString();
        }
        catch (IndexOutOfRangeException)
        {

        }

        if (this.InvokeRequired)
        {
            this.Invoke(new Action(() =>
            {
                if (_featureCollection.Alerts.Count > 0)
                    FilterAlerts();
            }));
        }
    }
}
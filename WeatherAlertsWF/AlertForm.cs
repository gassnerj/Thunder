using GeoJsonWeather;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WeatherAlertsWF
{
    public partial class AlertForm : Form
    {
        public AlertForm(Alert alert)
        {
            InitializeComponent();
            Text = alert.Event;
            headerPanel.BackColor = alert.AlertColor;
            headerPanel.ForeColor = alert.SecondaryColor;
            eventLabel.Text = alert.Event;
            effectiveLabel.Text = alert.Effective.ToString();
            label2.Text = alert.Description;

        }
    }
}

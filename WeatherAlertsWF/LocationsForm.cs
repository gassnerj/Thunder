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
    public partial class LocationsForm : Form
    {
        public IEnumerable<string> AllLocations { get; set; }
        public IEnumerable<string> UserLocations { get; set; }


        public LocationsForm()
        {
            InitializeComponent();

            AllLocations = new Models.States().AllStates;

            statesListbox.DataSource = AllLocations;
        }
    }
}

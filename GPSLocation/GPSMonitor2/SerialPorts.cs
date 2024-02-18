using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GPSMonitor;

namespace GPSMonitor2
{
    public partial class SerialPorts : Form
    {
        private GPSMonitorForm monitorForm;
        public SerialPorts(GPSMonitorForm gpsForm)
        {
            InitializeComponent();

            monitorForm = gpsForm;
            string[] ports = SerialPort.GetPortNames();
            checkedListBox1.Items.AddRange(ports);
            
            checkedListBox1.SelectionMode = SelectionMode.One;
            checkedListBox1.SelectedIndex = 2;
        }

        private void okBtn_Click(object sender, EventArgs e)
        {
            if (checkedListBox1.CheckedItems.Count == 1)
            {
                monitorForm.SerialPortName = checkedListBox1.SelectedItem.ToString();
                Close();
            } else
            {
                MessageBox.Show("You may only choose one serial port.", 
                    "Serial Port Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            Close(); 
        }
    }
}

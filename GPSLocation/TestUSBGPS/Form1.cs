using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TestUSBGPS
{
    public partial class Form1 : Form
    {
        private const int WM_DEVICECHANGE = 0x219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int DBT_DEVTYP_VOLUME = 0x00000002;
        private const int DBT_DEVTYP_OEM = 0x00000000;
        private const int DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;
        private const int DBT_DEVTYP_PORT = 0x00000003;

        public Form1()
        {
            InitializeComponent();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            switch (m.Msg)
            {
                case WM_DEVICECHANGE:
                    switch ((int)m.WParam)
                    {
                        case DBT_DEVICEARRIVAL:
                            listBox1.Items.Add("New Device Arrived");
                            int devType = Marshal.ReadInt32(m.LParam, 4);
                            if (devType == DBT_DEVTYP_PORT)
                            {
                                DevBroadcastPort port;
                                port = (DevBroadcastPort)Marshal.PtrToStructure(m.LParam,
                                    typeof(DevBroadcastPort));
                                listBox1.Items.Add("Name is " + port.Name);
                            }
                            break;

                        case DBT_DEVICEREMOVECOMPLETE:
                            listBox1.Items.Add("Device Removed");
                            break;

                    }

                    break;
            }
        }
    }
}
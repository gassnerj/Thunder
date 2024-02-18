using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TestUSBGPS
{
    [StructLayout(LayoutKind.Sequential)]
    public class DevBroadcastPort
    {
        public int Size;
        public int DeviceType;
        public int Reserved;
        public char Name;
    }
}

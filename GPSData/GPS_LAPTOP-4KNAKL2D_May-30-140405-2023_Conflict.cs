using GeoCoordinatePortable;
using System.IO.Ports;

namespace GPSData
{
    public class GPSMessageEventArgs : EventArgs
    {
        public NMEA NMEASentence { get; set; }

        public GPSMessageEventArgs(NMEA nmea)
        {
            NMEASentence = nmea;
        }
    }

    public class GPS : IGPS
    {
        public NMEA NMEASentence { get; private set; }

        public event EventHandler<GPSMessageEventArgs> MessageReceived;

        private SerialPort _serialPort;

        public GPS()
        {
            _serialPort= new SerialPort("COM2");
            _serialPort.DataReceived += _serialPort_DataReceived;
            NMEASentence = new NMEA();
        }

        private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort serialPort = (SerialPort)sender;

            //int bytesToRead = serialPort.BytesToRead;
            //byte[] buffer = new byte[bytesToRead];
            //serialPort.Read(buffer, 0, bytesToRead);
            //string receivedData = System.Text.Encoding.ASCII.GetString(buffer);

            //string[] sentences = receivedData.Split('\n');
            try
            {
                string receivedData = serialPort.ReadLine();

                if (receivedData.Contains("GPRMC") || receivedData.Contains("GPGGA"))
                {
                    NMEASentence.Parse(receivedData);
                    OnMessageReceived(new GPSMessageEventArgs(NMEASentence));
                }
            } catch (Exception ex)
            {

            }

        }

        public void Read()
        {
            try
            {
                _serialPort.Open();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public void StopReader()
        {
            if (_serialPort.IsOpen)
                _serialPort.Close();
        }

        protected virtual void OnMessageReceived(GPSMessageEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }
    }
}

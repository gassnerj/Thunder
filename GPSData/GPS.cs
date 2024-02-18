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
        private CancellationTokenSource _cancellationTokenSource;

        public GPS(string comPort)
        {
            _serialPort = new SerialPort(comPort);
            _serialPort.DataReceived += _serialPort_DataReceived;
            
            NMEASentence = new NMEA();
        }

        public void UpdatePortName(string comPort)
        {
            _serialPort!.PortName = comPort;
        }

        private async void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string receivedData = await ReadDataAsync(_cancellationTokenSource.Token);

                if (receivedData.Contains("GPRMC") || receivedData.Contains("GPGGA"))
                {
                    NMEASentence.Parse(receivedData);
                    OnMessageReceived(new GPSMessageEventArgs(NMEASentence));
                }
            } catch (Exception)
            {

            }
        }

        private async Task<string?> ReadDataAsync(CancellationToken cancellationToken)
        {
            var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return _serialPort.ReadLine();
                }
                catch (Exception) { return null; }
            }
            return null;
        }

        public void Read()
        {
            try
            {
                _serialPort.Open();
                _cancellationTokenSource= new CancellationTokenSource();
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
            _cancellationTokenSource.Cancel();
        }

        protected virtual void OnMessageReceived(GPSMessageEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }
    }
}

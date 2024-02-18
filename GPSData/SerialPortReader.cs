using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace GPSData
{
    public class SerialPortReader : IDisposable
    {
        private SerialPort _serialPort;
        private bool _disposed = false;

        public bool IsPortOpen => _serialPort.IsOpen;

        public SerialPortReader(string port)
        {
            _serialPort = new SerialPort(port);
            _serialPort.DataReceived += _serialPort_DataReceived;
        }

        private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort serialPort = (SerialPort)sender;

            int bytesToRead = serialPort.BytesToRead;
            byte[] buffer = new byte[bytesToRead];
            serialPort.Read(buffer, 0, bytesToRead);

            string receivedData = System.Text.Encoding.ASCII.GetString(buffer);
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
            finally
            {
                if (_serialPort.IsOpen)
                    _serialPort.Close();
            }
        }

        public void StopReading()
        {
            if (_serialPort.IsOpen)
                _serialPort.Close();
        }

        public IEnumerable<string> GetComPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                yield return port;
            }
        }

        public void Dispose()
        {
            _serialPort?.Close();
            _serialPort?.Dispose();
            _disposed = true;
        }

        ~SerialPortReader()
        {
            if (!_disposed)
            {
                Dispose();
            }
        }
    }
}

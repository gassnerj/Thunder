using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using GeoCoordinatePortable;

namespace GPSData
{
    public class NMEA : INMEA, INotifyPropertyChanged
    {
        private int _positionFix;
        private GeoCoordinate _geoCoordinate;
        private int _satellitesUsed;
        private string _status;

        private const double FEET_METER = 3.28084;
        private const double MPH_KNOTS = 1.15078;

        public string SentenceID { get; private set; } = string.Empty;
        private TimeOnly UTCTime { get; set; }
        private DateOnly UTCDate { get; set; }

        public DateTime UTC { get { return new DateTime(UTCDate.Year, UTCDate.Month, UTCDate.Day, UTCTime.Hour, UTCTime.Minute, UTCTime.Second); } }
        public double Latitude { get { return Math.Round(_geoCoordinate.Latitude, 6); } }
        public double Longitude { get { return Math.Round(_geoCoordinate.Longitude, 6); } }
        public int PositionFix
        {
            get { return _positionFix; }
            private set
            {
                if (value < 0 || value > 3)
                    throw new ArgumentOutOfRangeException("Position Fix must be from 0 - 3.");

                _positionFix = value;
            }
        }

        public int SatellitesUsed
        {
            get { return _satellitesUsed; }
            private set
            {
                if (value < 0 || value > 12)
                    throw new ArgumentOutOfRangeException("Valid satellites must be from 0-12.");

                _satellitesUsed = value;
            }
        }

        public string Status
        {
            get
            {
                if (_status == "A")
                    return "Valid Fix";
                else if (_status == "V")
                    return "Invalid Fix";
                else return "";
            }
            private set { _status = value; }
        }


        /// <summary>
        /// Horizontal dilution of precision
        /// </summary>
        public double HDOP { get; private set; }
        public double Altitude { get { return _geoCoordinate.Altitude; } }
        public double Speed { get { return _geoCoordinate.Speed; } }
        public double Course { get { return _geoCoordinate.Course; } }

        public NMEA(string gprmc)
        {
            _geoCoordinate = new GeoCoordinate();
            _status = "";
            Parse(gprmc);
        }

        public NMEA()
        {
            _geoCoordinate = new GeoCoordinate();
            _status = "";
        }

        public void Parse(string data)
        {
            string[] lineArr = data.Split(',');

            if (lineArr.Length > 0 && lineArr[0] == "$GPRMC")
            {
                try
                {
                    //Latitude
                    var dLat = Convert.ToDouble(lineArr[3]);
                    int pt = dLat.ToString().IndexOf('.');
                    var degreesLat = Convert.ToDouble(dLat.ToString()[..(pt - 2)]);
                    var minutesLat = Convert.ToDouble(dLat.ToString()[(pt - 2)..]);
                    double DecDegsLat = degreesLat + (minutesLat / 60.0);
                    _geoCoordinate.Latitude = DecDegsLat;

                    //Longitude
                    var dLon = Convert.ToDouble(lineArr[5]);
                    pt = dLon.ToString().IndexOf('.');
                    var degreesLon = Convert.ToDouble(dLon.ToString()[..(pt - 2)]);
                    var minutesLon = Convert.ToDouble(dLon.ToString()[(pt - 2)..]);
                    double DecDegsLon = (degreesLon + (minutesLon / 60.0)) * -1;
                    _geoCoordinate.Longitude = DecDegsLon;

                    //Speed

                    _geoCoordinate.Speed = Convert.ToDouble(lineArr[7]) * MPH_KNOTS;
                    _geoCoordinate.Course = Convert.ToDouble(lineArr[8]);


                    // Date Time
                    UTCTime = GetTimeFromNMEA(lineArr[1]);
                    UTCDate = DateOnly.ParseExact(lineArr[9], "ddMMyy");

                    //Status Fix
                    Status = lineArr[2];
                }
                catch (ArgumentOutOfRangeException)
                {
                    //
                }
                catch (Exception)
                {
                    //
                }
            }

            if (lineArr[0] == "$GPGGA")
            {
                if (lineArr.Length > 11 && lineArr[6] == "1")
                {
                    _ = double.TryParse(lineArr[9], out double alt);
                    _geoCoordinate.Altitude = alt * FEET_METER;
                }
            }
        }

        private TimeOnly GetTimeFromNMEA(string nmeaUtcTime)
        {
            // Parse the NMEA UTC time string
            int hours = int.Parse(nmeaUtcTime.Substring(0, 2));
            int minutes = int.Parse(nmeaUtcTime.Substring(2, 2));
            int seconds = int.Parse(nmeaUtcTime.Substring(4, 2));
            int milliseconds = int.Parse(nmeaUtcTime.Substring(7, 3));

            // Create a DateTimeOffset object with the parsed UTC time
            DateTimeOffset utcDateTime = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
                                                           hours, minutes, seconds, milliseconds, TimeSpan.Zero);

            return TimeOnly.Parse(utcDateTime.TimeOfDay.ToString());
        }

        public string GetCardinalDirection(double heading)
        {
            string direction;

            if (heading >= 337.5 || heading < 22.5)
                direction = "N";
            else if (heading >= 22.5 && heading < 67.5)
                direction = "NE";
            else if (heading >= 67.5 && heading < 112.5)
                direction = "E";
            else if (heading >= 112.5 && heading < 157.5)
                direction = "SE";
            else if (heading >= 157.5 && heading < 202.5)
                direction = "S";
            else if (heading >= 202.5 && heading < 247.5)
                direction = "SW";
            else if (heading >= 247.5 && heading < 292.5)
                direction = "W";
            else
                direction = "NW";

            return direction;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}

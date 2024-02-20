using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using GeoCoordinatePortable;

namespace GPSData
{
    public sealed class NMEA : INMEA, INotifyPropertyChanged
    {
        private int _positionFix;
        private readonly GeoCoordinate _geoCoordinate;
        private int _satellitesUsed;
        private string _status;

        private const double _FEET_METER = 3.28084;
        private const double _MPH_KNOTS = 1.15078;

        public string SentenceID { get; private set; } = string.Empty;
        private TimeOnly UTCTime { get; set; }
        private DateOnly UTCDate { get; set; }

        public DateTime UTC => new(UTCDate.Year, UTCDate.Month, UTCDate.Day, UTCTime.Hour, UTCTime.Minute, UTCTime.Second);
        public double Latitude => Math.Round(_geoCoordinate.Latitude, 4);
        public double Longitude => Math.Round(_geoCoordinate.Longitude, 4);

        public int PositionFix
        {
            get => _positionFix;
            private set
            {
                if (value is < 0 or > 3)
                    throw new ArgumentOutOfRangeException($"Position Fix must be from 0 - 3.");

                if (value == _positionFix)
                    return;
                _positionFix = value;
                OnPropertyChanged();
            }
        }

        public int SatellitesUsed
        {
            get => _satellitesUsed;
            private set
            {
                if (value is < 0 or > 12)
                    throw new ArgumentOutOfRangeException($"Valid satellites must be from 0-12.");

                _satellitesUsed = value;
            }
        }

        public string Status
        {
            get
            {
                return _status switch
                {
                    "A" => "Valid Fix",
                    "V" => "Invalid Fix",
                    _   => ""
                };
            }
            private set => _status = value;
        }


        /// <summary>
        /// Horizontal dilution of precision
        /// </summary>
        public double HDOP { get; private set; }

        public double Altitude => _geoCoordinate.Altitude;
        public double Speed => _geoCoordinate.Speed;
        public double Course => _geoCoordinate.Course;

        public NMEA(string gprmc)
        {
            _geoCoordinate = new GeoCoordinate();
            _status        = "";
            Parse(gprmc);
        }

        public NMEA()
        {
            _geoCoordinate = new GeoCoordinate();
            _status        = "";
        }

        public void Parse(string data)
        {
            string[] lineArr = data.Split(',');

            if (lineArr.Length > 0 && lineArr[0] == "$GPRMC")
            {
                try
                {
                    //Latitude
                    var    dLat       = Convert.ToDouble(lineArr[3]);
                    int    pt         = dLat.ToString(CultureInfo.CurrentCulture).IndexOf('.');
                    var    degreesLat = Convert.ToDouble(dLat.ToString(CultureInfo.CurrentCulture)[..(pt - 2)]);
                    var    minutesLat = Convert.ToDouble(dLat.ToString(CultureInfo.CurrentCulture)[(pt - 2)..]);
                    double decDegsLat = degreesLat + (minutesLat / 60.0);
                    _geoCoordinate.Latitude = decDegsLat;

                    //Longitude
                    var dLon = Convert.ToDouble(lineArr[5]);
                    pt = dLon.ToString(CultureInfo.CurrentCulture).IndexOf('.');
                    var    degreesLon = Convert.ToDouble(dLon.ToString(CultureInfo.CurrentCulture)[..(pt - 2)]);
                    var    minutesLon = Convert.ToDouble(dLon.ToString(CultureInfo.CurrentCulture)[(pt - 2)..]);
                    double decDegsLon = (degreesLon + (minutesLon / 60.0)) * -1;
                    _geoCoordinate.Longitude = decDegsLon;

                    //Speed

                    _geoCoordinate.Speed  = Convert.ToDouble(lineArr[7]) * _MPH_KNOTS;
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

            if (lineArr[0] != "$GPGGA")
                return;
            if (lineArr.Length <= 11 || lineArr[6] != "1")
                return;
            _                       = double.TryParse(lineArr[9], out double alt);
            _geoCoordinate.Altitude = alt * _FEET_METER;
        }

        private static TimeOnly GetTimeFromNMEA(string nmeaUtcTime)
        {
            // Parse the NMEA UTC time string
            int hours        = int.Parse(nmeaUtcTime[..2]);
            int minutes      = int.Parse(nmeaUtcTime.Substring(2, 2));
            int seconds      = int.Parse(nmeaUtcTime.Substring(4, 2));
            int milliseconds = int.Parse(nmeaUtcTime.Substring(7, 3));

            // Create a DateTimeOffset object with the parsed UTC time
            DateTimeOffset utcDateTime = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
                hours, minutes, seconds, milliseconds, TimeSpan.Zero);

            return TimeOnly.Parse(utcDateTime.TimeOfDay.ToString());
        }

        public string GetCardinalDirection(double heading)
        {
            string direction;

            switch (heading)
            {
                case >= 337.5:
                case < 22.5:
                    direction = "N";
                    break;
                case >= 22.5 and < 67.5:
                    direction = "NE";
                    break;
                case >= 67.5 and < 112.5:
                    direction = "E";
                    break;
                case >= 112.5 and < 157.5:
                    direction = "SE";
                    break;
                case >= 157.5 and < 202.5:
                    direction = "S";
                    break;
                case >= 202.5 and < 247.5:
                    direction = "SW";
                    break;
                case >= 247.5 and < 292.5:
                    direction = "W";
                    break;
                default:
                    direction = "NW";
                    break;
            }

            return direction;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
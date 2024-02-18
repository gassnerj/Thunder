using GeoCoordinatePortable;

namespace GPSData
{
    public interface IGPS
    {
        NMEA NMEASentence { get; }

        void Read();
    }
}
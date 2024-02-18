
namespace GPSData
{
    public interface INMEA
    {
        double Altitude { get; }
        double Course { get; }
        double HDOP { get; }
        double Latitude { get; }
        double Longitude { get; }
        int PositionFix { get; }
        int SatellitesUsed { get; }
        string SentenceID { get; }
        double Speed { get; }
        string Status { get; }
        DateTime UTC { get; }

        string GetCardinalDirection(double heading);
        void Parse(string data);
    }
}
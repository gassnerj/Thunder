namespace GPSData
{
    public interface IGeoCodeLocation
    {
        string City { get; set; }
        string County { get; set; }
        string Road { get; set; }
        string State { get; set; }
        string Heading { get; set; }
    }
}
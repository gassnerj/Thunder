namespace MeteorologyCore;

public interface IASOSStation
{
    string Identifier { get; set; }
    string Name { get; set; }
    string TimeZone { get; set; }
    string URL { get; set; }
}
namespace MeteorologyCore;

internal class Metar
{
    public string IcaoStation { get; private set; }
    public DateTime Timestamp { get; private set; }

    public Metar Parse(string m)
    {
        string? metar = m.Split(',')[1];
        return new Metar();
    }
}
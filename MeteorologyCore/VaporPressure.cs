namespace MeteorologyCore;

public class VaporPressure
{
    public bool Saturated { get; private set; }
    public Millibar Value { get; private set; }

    public VaporPressure(double v)
    {
        this.Value = v;
    }

    public static VaporPressure Calculate(Celsius t, DewPoint dt = null)
    {
        double n = (7.5 * t) / (237.3 + t);
        double es = 6.11 * Math.Pow(10, n);
        return new VaporPressure(es);
    }

    public static implicit operator VaporPressure(double t)
    {
        return new VaporPressure(t);
    }

    public static implicit operator double(VaporPressure t)
    {
        return t.Value;
    }
}

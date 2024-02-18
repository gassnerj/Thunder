namespace MeteorologyCore;

public class Pressure
{
    public double Value { get; set; }

    public Pressure(double p)
    {
        this.Value = p;
    }
}

public class Millibar
{
    public double Value;

    public Millibar(double v)
    {
        this.Value = v;
    }

    public static implicit operator Millibar(double v)
    {
        return new Millibar(v);
    }

    public static implicit operator double(Millibar t)
    {
        return t.Value;
    }
}

public class Inch
{
    public double Value;
}

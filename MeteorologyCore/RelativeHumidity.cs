namespace MeteorologyCore;

public class RelativeHumidity : IHumidityCalculator
{
    public double Value { get; set; }

    public RelativeHumidity(double v)
    {
        this.Value = v;
    }

    public RelativeHumidity Calculate(VaporPressure es, VaporPressure e)
    {
        return e / es * 100;
    }

    public static RelativeHumidity operator +(RelativeHumidity f, double v)
    {
        return new RelativeHumidity(f.Value + v);
    }

    public static RelativeHumidity operator *(RelativeHumidity f, double v)
    {
        return new RelativeHumidity(f.Value * v);
    }

    public static RelativeHumidity operator -(RelativeHumidity f, double v)
    {
        return new RelativeHumidity(f.Value - v);
    }

    public static implicit operator double(RelativeHumidity t)
    {
        return t.Value;
    }

    public static implicit operator RelativeHumidity(double t)
    {
        return new RelativeHumidity(t);
    }
}

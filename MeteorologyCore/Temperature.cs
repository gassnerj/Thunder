namespace MeteorologyCore;

public class Temperature
{
    public double Value { get; set; }
    public const double ABSOLUTEZERO = 273.15;
    public const double FREEZINGPOINTWATER = 32;

    public Temperature()
    {

    }

    public Temperature(double t)
    {
        this.Value = t;
    }
}

public class Celsius : Temperature
{
    public Celsius(double temperature)
    {
        this.Value = temperature;
    }

    public Fahrenheit ToFahrenheit()
    {
        return new Fahrenheit(this.Value * 1.8 + FREEZINGPOINTWATER);
    }

    public Kelvin ToKelvin()
    {
        return new Kelvin(this.Value + ABSOLUTEZERO);
    }

    public static Celsius operator +(Celsius f, double v)
    {
        return new Celsius(f.Value + v);
    }

    public static Celsius operator *(Celsius f, double v)
    {
        return new Celsius(f.Value * v);
    }

    public static Celsius operator -(Celsius f, double v)
    {
        return new Celsius(f.Value - v);
    }

    public static implicit operator Celsius(double v)
    {
        return new Celsius(v);
    }

    public static implicit operator double(Celsius t)
    {
        return t.Value;
    }
}

public class Fahrenheit : Temperature
{
    public Fahrenheit(double temperature)
    {
        Value = temperature;
    }

    public Celsius ToCelsius()
    {
        return new Celsius((this.Value - FREEZINGPOINTWATER) * .5556);
    }

    public Kelvin ToKelvin()
    {
        return new Kelvin(this.ToCelsius() + ABSOLUTEZERO);
    }

    public static Fahrenheit operator +(Fahrenheit f, double v)
    {
        return new Fahrenheit(f.Value + v);
    }

    public static Fahrenheit operator *(Fahrenheit f, double v)
    {
        return new Fahrenheit(f.Value * v);
    }

    public static Fahrenheit operator -(Fahrenheit f, double v)
    {
        return new Fahrenheit(f.Value - v);
    }

    public static implicit operator double(Fahrenheit t)
    {
        return t.Value;
    }

    public static implicit operator Fahrenheit(double v)
    {
        return new Fahrenheit(v);
    }
}

public class Kelvin : Temperature
{
    public Kelvin(double temperature)
    {
        this.Value = temperature;
    }

    public Fahrenheit ToFahrenheit()
    {
        return new Fahrenheit((double)this.ToCelsius().ToFahrenheit());
    }

    public Celsius ToCelsius()
    {
        return new Celsius(this.Value - ABSOLUTEZERO);
    }

    public static Kelvin operator +(Kelvin f, double v)
    {
        return new Kelvin(f.Value + v);
    }

    public static Kelvin operator *(Kelvin f, double v)
    {
        return new Kelvin(f.Value * v);
    }

    public static Kelvin operator -(Kelvin f, double v)
    {
        return new Kelvin(f.Value - v);
    }

    public static implicit operator Kelvin(double v)
    {
        return new Kelvin(v);
    }

    public static implicit operator double(Kelvin t)
    {
        return t.Value;
    }
}

public class HeatIndex : Temperature
{
    public HeatIndex()
    {

    }

    public HeatIndex(double temperature)
    {
        this.Value = temperature;
    }

    public static HeatIndex Calculate(Fahrenheit t, RelativeHumidity rH)
    {
        if (t > 79)
        {
            double temp = t.Value;

            double hi = -42.379 + (2.04901523 * temp) + (10.14333127 * (double)rH)
                - (0.22475541 * temp * (double)rH) - (6.83783 * Math.Pow(10, -3) * Math.Pow(temp, 2))
                - (5.481717 * Math.Pow(10, -2) * Math.Pow((double)rH, 2)) + (1.22874 * Math.Pow(10, -3) * Math.Pow(temp, 2) * (double)rH)
                                                                          + (8.5282 * Math.Pow(10, -4) * temp * Math.Pow((double)rH, 2)) - (1.99 * Math.Pow(10, -6) * Math.Pow(temp, 2) * Math.Pow((double)rH, 2));
            return hi;
        }
        else
        {
            return null;
        }

    }

    public static HeatIndex Calculate(Celsius t, RelativeHumidity rH)
    {
        return HeatIndex.Calculate(t.ToFahrenheit(), rH);
    }

    public static implicit operator HeatIndex(double v)
    {
        return new HeatIndex(v);
    }

    public static implicit operator double(HeatIndex t)
    {
        return t.Value;
    }
}

public class WindChill
{
    private WindChill windChill;

    public double Value { get; set; }

    public WindChill(double v)
    {
        this.Value = v;
    }

    public WindChill(WindChill windChill)
    {
        this.windChill = windChill;
    }

    public static WindChill Calculate(Fahrenheit f, double windSpeed)
    {
        if (f < 41)
        {
            double wc = 35.74 + (.6215 * (double)f) - (35.75 * Math.Pow(windSpeed, .16))
                        + (.4275 * (double)f * Math.Pow(windSpeed, .16));

            return new WindChill(wc);
        }
        else
        {
            return null;
        }
    }

    public static WindChill operator +(WindChill f, double v)
    {
        return new WindChill(f + v);
    }

    public static WindChill operator *(WindChill f, double v)
    {
        return new WindChill(f * v);
    }

    public static WindChill operator -(WindChill f, double v)
    {
        return new WindChill(f - v);
    }

    public static implicit operator WindChill(double v)
    {
        return new WindChill(v);
    }

    public static implicit operator double(WindChill t)
    {
        return t.Value;
    }
}

public class DewPoint
{
    public Celsius Value { get; set; }

    public DewPoint()
    {

    }

    public DewPoint(double v)
    {
        this.Value = v;
    }

    public static DewPoint Calculate(Celsius t, RelativeHumidity rh, Pressure p)
    {
        double es = VaporPressure.Calculate(t);
        double numerator = 237.3 * Math.Log((es * rh) / 611);
        double denominator = 7.5 * Math.Log(10) - Math.Log((es * rh) / 611);
        return new DewPoint(numerator / denominator);
    }
}
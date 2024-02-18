namespace MeteorologyCore;

public class Celsius : ITemperature
{
    public double Value { get; }

    public Celsius(double temperature)
    {
        Value = temperature;
    }

    public ITemperature ToFahrenheit() => new Fahrenheit(Value * 9 / 5 + 32);
    public ITemperature ToKelvin()  => new Kelvin(Value + 273.15);
    public ITemperature ToCelsius() => this;
    public override string ToString() => $"{Math.Round(Value)} °C";
}

public class Fahrenheit : ITemperature
{
    public double Value { get; }

    public Fahrenheit(double temperature)
    {
        Value = temperature;
    }

    public ITemperature ToCelsius() => new Celsius((Value - 32) * 5 / 9);

    public ITemperature ToKelvin()    => new Kelvin((Value - 32) * 5 / 9 + 273.15);
    public ITemperature ToFahrenheit() => this;

    public override string ToString() => $"{Math.Round(Value)} °F";
}

public class Kelvin : ITemperature
{
    public double Value { get; }

    public Kelvin(double temperature)
    {
        Value = temperature;
    }

    public ITemperature ToCelsius() => new Celsius(Value - 273.15);

    public ITemperature ToFahrenheit() => new Fahrenheit((Value - 273.15) * 9 / 5 + 32);
    public ITemperature ToKelvin()     => this;

    public override string ToString() => $"{Math.Round(Value)} K";
}

public class DewPointCalculator
{
    public Celsius Calculate(Celsius temperature, double relativeHumidity)
    {
        double es = new VaporPressure().Calculate(temperature);
        double numerator = 237.3 * Math.Log((es * relativeHumidity) / 611.2);
        double denominator = 7.5 * Math.Log(10) - Math.Log((es * relativeHumidity) / 611.2);
        return new Celsius(numerator / denominator);
    }
}

public interface IHeatIndex
{
    ITemperature? Calculate(ITemperature temperature, double relativeHumidity);
}

public class HeatIndexCalculator : IHeatIndex
{
    public ITemperature? Calculate(ITemperature temperature, double relativeHumidity)
    {
        if (temperature.ToCelsius().Value > 79)
        {
            double temp = temperature.ToFahrenheit().Value;
            double hi = -42.379 + (2.04901523 * temp) + (10.14333127 * relativeHumidity)
                - (0.22475541 * temp * relativeHumidity) - (6.83783 * Math.Pow(10, -3) * Math.Pow(temp, 2))
                - (5.481717 * Math.Pow(10, -2) * Math.Pow(relativeHumidity, 2))
                + (1.22874 * Math.Pow(10, -3) * Math.Pow(temp, 2) * relativeHumidity)
                + (8.5282 * Math.Pow(10, -4) * temp * Math.Pow(relativeHumidity, 2))
                - (1.99 * Math.Pow(10, -6) * Math.Pow(temp, 2) * Math.Pow(relativeHumidity, 2));
            return new Fahrenheit(hi);
        }
        else
        {
            return null;
        }
    }
}

public interface IWindChill
{
    ITemperature? Calculate(ITemperature temperature, double windSpeed);
}

public class WindChillCalculator : IWindChill
{
    public ITemperature? Calculate(ITemperature temperature, double windSpeed)
    {
        if (temperature.ToFahrenheit().Value < 41)
        {
            double wc = 35.74 + (0.6215 * temperature.ToFahrenheit().Value) - (35.75 * Math.Pow(windSpeed, 0.16))
                + (0.4275 * temperature.ToFahrenheit().Value * Math.Pow(windSpeed, 0.16));
            return new Fahrenheit(wc);
        }
        else
        {
            return null;
        }
    }
}

namespace MeteorologyCore;

/// <summary>
/// See: https://www.weather.gov/media/epz/wxcalc/vaporPressure.pdf
/// </summary>
public class VaporPressure
{
    public double Actual { get; private set; }
    public double Saturated { get; private set; }

    /// <summary>
    /// Calculates vapor pressure from dew point and air temp.
    /// </summary>
    /// <param name="dewPointTemperature"></param>
    /// <param name="airTemperature"></param>
    /// <returns></returns>
    public VaporPressure Calculate(Celsius dewPointTemperature, Celsius airTemperature)
    {
        Actual    = Calculate(dewPointTemperature);
        Saturated = Calculate(airTemperature);
        return this;
    }

    public double Calculate(Celsius temperature)
    {
        double n  = (7.5 * temperature.Value) / (237.3 + temperature.Value);
        return 6.11 * Math.Pow(10, n);
    }
}

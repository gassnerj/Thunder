namespace MeteorologyCore;

public static class RelativeHumidityCalculator
{
    public static double Calculate(VaporPressure vaporPressure)
    {
        return vaporPressure.Actual / vaporPressure.Saturated * 100;
    }
    
    public static string ToString(double relativeHumidity)
    {
        return $"{relativeHumidity:F0}%";
    }
}

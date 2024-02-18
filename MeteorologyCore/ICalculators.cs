namespace MeteorologyCore;

public interface IHeatIndexCalculator
{
    HeatIndex Calculate(Fahrenheit temperature, RelativeHumidity humidity);
}

public interface IDewPointCalculator
{
    DewPoint Calculate(Celsius temperature, RelativeHumidity humidity, Pressure pressure);
}

public interface IWindChillCalculator
{
    WindChill Calculate(Fahrenheit temperature, double windSpeed);
}

public interface IHumidityCalculator
{
    RelativeHumidity Calculate(VaporPressure es, VaporPressure e);
}

public class MyHumidityCalculator : IHumidityCalculator
{
    public RelativeHumidity Calculate(VaporPressure es, VaporPressure e)
    {
        return new RelativeHumidity(es.Value + e.Value);
    }
}
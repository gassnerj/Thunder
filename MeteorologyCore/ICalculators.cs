/*namespace MeteorologyCore;

public interface IHeatIndexCalculator
{
    HeatIndex Calculate(Fahrenheit temperature, RelativeHumidityCalculator humidityCalculator);
}

public interface IDewPointCalculator
{
    DewPoint Calculate(Celsius temperature, RelativeHumidityCalculator humidityCalculator, Pressure pressure);
}

public interface IWindChillCalculator
{
    WindChill Calculate(Fahrenheit temperature, double windSpeed);
}

public interface IHumidityCalculator
{
    RelativeHumidityCalculator Calculate(VaporPressure es, VaporPressure e);
}

public class MyHumidityCalculator : IHumidityCalculator
{
    public RelativeHumidityCalculator Calculate(VaporPressure es, VaporPressure e)
    {
        return new RelativeHumidityCalculator(es.Value + e.Value);
    }
}*/
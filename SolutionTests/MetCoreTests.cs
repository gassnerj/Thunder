using MeteorologyCore;

namespace SolutionTests;

public class MetCoreTests
{
    [Fact]
    public void DewPointCalculatorTest()
    {
        var calculator = new DewPointCalculator();
        Celsius dewPoint = calculator.Calculate(new Celsius(40), 65);
        
        Assert.Equal(32.1, Math.Round(dewPoint.Value, 1));
    }

    [Fact]
    public void RelativeHumidityCalculatorTest()
    {
        VaporPressure vaporPressure = new VaporPressure().Calculate(new Celsius(25), new Celsius(35));
        double relativeHumidity = RelativeHumidityCalculator.Calculate(vaporPressure);
        Assert.Equal(56, Math.Round(relativeHumidity));
    }
}
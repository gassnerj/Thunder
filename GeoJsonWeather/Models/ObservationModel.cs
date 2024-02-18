using System;
using System.Threading.Tasks;
using GeoJsonWeather.Stations;
using MeteorologyCore;

namespace GeoJsonWeather.Models;

public class ObservationModel : IApiRetreivable
{
    public ITemperature Temperature { get; set; }
    public ITemperature HeatIndex { get; set; }
    public ITemperature DewPoint { get; set; }
    public double RelativeHumidity { get; set; }
    public Wind Wind { get; set; } = null!;
    public ITemperature WindChill { get; set; }
    public Pressure BarometricPressure { get; set; }
    public Pressure SeaLevelPressure { get; set; }
    public DateTime Timestamp { get; set; }
    public Task<string> GetData()
    {
        throw new NotImplementedException();
    }
}
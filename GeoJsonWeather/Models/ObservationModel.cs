using System;
using System.Threading.Tasks;
using GeoJsonWeather.Api;
using MeteorologyCore;

namespace GeoJsonWeather.Models;

public class ObservationModel
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
}
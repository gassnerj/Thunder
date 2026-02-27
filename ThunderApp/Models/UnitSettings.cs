namespace ThunderApp.Models;

public enum TemperatureUnit
{
    Fahrenheit,
    Celsius
}

public enum WindSpeedUnit
{
    Mph,
    Kts,
    Mps
}

public enum PressureUnit
{
    InHg,
    HPa,
    Pa
}

public enum MapTheme
{
    Light,
    Dark
}

public sealed class UnitSettings
{
    public TemperatureUnit TemperatureUnit { get; set; } = TemperatureUnit.Fahrenheit;
    public WindSpeedUnit WindSpeedUnit { get; set; } = WindSpeedUnit.Mph;
    public PressureUnit PressureUnit { get; set; } = PressureUnit.InHg;
    public MapTheme MapTheme { get; set; } = MapTheme.Dark;

    public UnitSettings Clone() => new()
    {
        TemperatureUnit = TemperatureUnit,
        WindSpeedUnit = WindSpeedUnit,
        PressureUnit = PressureUnit,
        MapTheme = MapTheme
    };
}

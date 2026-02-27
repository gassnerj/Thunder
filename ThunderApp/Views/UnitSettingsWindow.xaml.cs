using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ThunderApp.Models;

namespace ThunderApp.Views;

public partial class UnitSettingsWindow : Window
{
    private readonly UnitSettings _working;

    public UnitSettingsWindow(UnitSettings current)
    {
        InitializeComponent();
        _working = current.Clone();

        TemperatureUnitCombo.ItemsSource = Enum.GetValues(typeof(TemperatureUnit));
        WindSpeedUnitCombo.ItemsSource = Enum.GetValues(typeof(WindSpeedUnit));
        PressureUnitCombo.ItemsSource = Enum.GetValues(typeof(PressureUnit));
        ObservationSourceCombo.ItemsSource = Enum.GetValues(typeof(WeatherObservationSource));
        MapThemeCombo.ItemsSource = Enum.GetValues(typeof(MapTheme));

        TemperatureUnitCombo.SelectedItem = _working.TemperatureUnit;
        WindSpeedUnitCombo.SelectedItem = _working.WindSpeedUnit;
        PressureUnitCombo.SelectedItem = _working.PressureUnit;
        ObservationSourceCombo.SelectedItem = _working.ObservationSource;
        MapThemeCombo.SelectedItem = _working.MapTheme;
        MapboxTokenTextBox.Text = _working.MapboxAccessToken ?? "";

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
                e.Handled = true;
            }
        };
    }

    public UnitSettings Result => _working.Clone();

    private void Minimize_OnClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_OnClick(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Cancel_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        _working.TemperatureUnit = GetSelected<TemperatureUnit>(TemperatureUnitCombo, TemperatureUnit.Fahrenheit);
        _working.WindSpeedUnit = GetSelected<WindSpeedUnit>(WindSpeedUnitCombo, WindSpeedUnit.Mph);
        _working.PressureUnit = GetSelected<PressureUnit>(PressureUnitCombo, PressureUnit.InHg);
        _working.ObservationSource = GetSelected<WeatherObservationSource>(ObservationSourceCombo, WeatherObservationSource.NearestAsos);
        _working.MapTheme = GetSelected<MapTheme>(MapThemeCombo, MapTheme.Dark);
        _working.MapboxAccessToken = MapboxTokenTextBox.Text?.Trim() ?? "";

        DialogResult = true;
        Close();
    }

    private static T GetSelected<T>(ComboBox combo, T fallback)
    {
        if (combo.SelectedItem is T val) return val;
        return fallback;
    }
}

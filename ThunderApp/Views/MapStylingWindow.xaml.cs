using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using ThunderApp.Models;
using ThunderApp.Services;
using ThunderApp.ViewModels;
using MessageBox = System.Windows.MessageBox;

namespace ThunderApp.Views;

public partial class MapStylingWindow : Window
{
    private readonly MapStylingViewModel _vm;

    public MapStylingWindow(AlertFilterSettings settings)
    {
        InitializeComponent();

        // Ensure the full official palette is loaded before the VM snapshots it.
        OfficialHazardPaletteLoader.EnsureLoaded();

        _vm = new MapStylingViewModel(settings);
        DataContext = _vm;

        // Simple key bindings (old school, but handy)
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
            else if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Save_OnClick(this, new RoutedEventArgs());
                e.Handled = true;
            }
        };
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsClickOnCaptionButton(e.OriginalSource as DependencyObject)) return;
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        try
        {
            DragMove();
        }
        catch
        {
            // ignore
        }
    }

    
    private static bool IsClickOnCaptionButton(DependencyObject? original)
    {
        if (original is null) return false;

        // If the user clicked a button in the title bar, don't start a DragMove.
        var cur = original;
        while (cur is not null)
        {
            if (cur is System.Windows.Controls.Primitives.ButtonBase) return true;
            cur = System.Windows.Media.VisualTreeHelper.GetParent(cur);
        }
        return false;
    }

private void Minimize_OnClick(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

    private void Maximize_OnClick(object sender, RoutedEventArgs e) => ToggleMaximize();

    private void ToggleMaximize()
    {
        if (WindowState == WindowState.Maximized)
            SystemCommands.RestoreWindow(this);
        else
            SystemCommands.MaximizeWindow(this);
    }

    private void Refresh_OnClick(object sender, RoutedEventArgs e)
    {
        OfficialHazardPaletteLoader.EnsureLoaded();
        _vm.RefreshItems();
    }

    private void About_OnClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Thunder — Map Styling\n\nUses the official Weather.gov Hazards Map palette (www/hazard-colors.json).\nCustom overrides are stored in your settings.",
            "About",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ClearCustom_OnClick(object sender, RoutedEventArgs e) => _vm.ClearCustomColors();

    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        _vm.SaveToSettings();
        DialogResult = true;
        Close();
    }



private void PickColor_OnClick(object sender, RoutedEventArgs e)
{
    if (sender is not System.Windows.Controls.Button btn) return;
    if (btn.DataContext is not HazardColorItem item) return;

    var dlg = new ColorDialog
    {
        FullOpen = true
    };

    // Seed dialog with current color if possible (custom first, then official)
    try
    {
        var seed = (item.CustomHex ?? item.OfficialHex)?.Trim();
        if (!string.IsNullOrWhiteSpace(seed) && seed.StartsWith("#") && (seed.Length == 7 || seed.Length == 9))
        {
            // ColorDialog uses ARGB; handle #RRGGBB and #AARRGGBB
            int a = 255, r = 0, g = 0, b = 0;
            if (seed.Length == 7)
            {
                r = int.Parse(seed.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
                g = int.Parse(seed.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                b = int.Parse(seed.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
            }
            else
            {
                a = int.Parse(seed.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
                r = int.Parse(seed.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                g = int.Parse(seed.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
                b = int.Parse(seed.Substring(7, 2), System.Globalization.NumberStyles.HexNumber);
            }
            dlg.Color = System.Drawing.Color.FromArgb(a, r, g, b);
        }
    }
    catch
    {
        // ignore seed parse errors
    }

    if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

    // Store as #RRGGBB (opaque) unless alpha < 255
    var c = dlg.Color;
    item.CustomHex = (c.A < 255)
        ? $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}"
        : $"#{c.R:X2}{c.G:X2}{c.B:X2}";
}

    private void Close_OnClick(object sender, RoutedEventArgs e) => Close();
}

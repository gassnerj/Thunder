using System.Windows;
using ThunderApp.Models;
using ThunderApp.ViewModels;

namespace ThunderApp.Views;

public partial class MapStylingWindow : Window
{
    private readonly MapStylingViewModel _vm;

    public MapStylingWindow(AlertFilterSettings settings)
    {
        InitializeComponent();
        _vm = new MapStylingViewModel(settings);
        DataContext = _vm;
    }

    private void ClearCustom_OnClick(object sender, RoutedEventArgs e)
    {
        _vm.ClearCustomColors();
    }

    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        _vm.SaveToSettings();
        DialogResult = true;
        Close();
    }

    private void Close_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

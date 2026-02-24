using CommunityToolkit.Mvvm.ComponentModel;
using ThunderApp.Models;

namespace ThunderApp.ViewModels;

public partial class AlertTypeToggleViewModel : ObservableObject
{
    public string EventName { get; }
    public AlertCategory Category { get; }
    public AlertLifecycle Lifecycle { get; }

    [ObservableProperty] private bool isEnabled = true;

    public AlertTypeToggleViewModel(AlertTypeDefinition def, bool enabled)
    {
        EventName = def.EventName;
        Category = def.Category;
        Lifecycle = def.Lifecycle;
        IsEnabled = enabled;
    }
}
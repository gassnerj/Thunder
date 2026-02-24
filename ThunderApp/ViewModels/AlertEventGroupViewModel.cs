using System.Collections.ObjectModel;
using ThunderApp.Models;

namespace ThunderApp.ViewModels;

public sealed class AlertEventGroupViewModel(AlertLifecycle lifecycle)
{
    public AlertLifecycle Lifecycle { get; } = lifecycle;
    public ObservableCollection<AlertEventToggleItem> Events { get; } = new();
}
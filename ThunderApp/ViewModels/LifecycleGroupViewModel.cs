using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ThunderApp.Models;

namespace ThunderApp.ViewModels;

public partial class LifecycleGroupViewModel : ObservableObject
{
    public AlertLifecycle Lifecycle { get; }

    public string Title => Lifecycle switch
    {
        AlertLifecycle.ShortFusedWarnings => "Short-Fused Warnings",
        AlertLifecycle.LongFusedWarnings  => "Long-Fused Warnings",
        AlertLifecycle.Watches            => "Watches",
        AlertLifecycle.Advisories         => "Advisories",
        AlertLifecycle.Statements         => "Statements",
        AlertLifecycle.Discussions        => "Discussions",
        AlertLifecycle.Outlooks           => "Outlooks",
        _ => Lifecycle.ToString()
    };

    public ObservableCollection<AlertTypeToggleViewModel> Events { get; } = [];

    public LifecycleGroupViewModel(AlertLifecycle lifecycle)
    {
        Lifecycle = lifecycle;
    }
}
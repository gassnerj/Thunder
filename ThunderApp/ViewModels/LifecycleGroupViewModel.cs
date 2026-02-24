using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
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

    public ICollectionView EventsView { get; }

    [ObservableProperty] private AlertCategory selectedCategory = AlertCategory.Severe;

    [ObservableProperty] private bool hasVisibleEvents;

    public LifecycleGroupViewModel(AlertLifecycle lifecycle)
    {
        Lifecycle = lifecycle;

        EventsView = CollectionViewSource.GetDefaultView(Events);
        EventsView.Filter = FilterEvent;
        UpdateHasVisibleEvents();
    }

    public void SetSelectedCategory(AlertCategory category)
    {
        SelectedCategory = category;
        EventsView.Refresh();
        UpdateHasVisibleEvents();
    }

    private bool FilterEvent(object obj)
    {
        if (obj is not AlertTypeToggleViewModel vm) return false;

        // Show only the toggles for the selected category, WF-style.
        return vm.Category == SelectedCategory;
    }

    private void UpdateHasVisibleEvents()
    {
        // cheap enough; groups are small
        HasVisibleEvents = EventsView.Cast<object>().Any();
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoJsonWeather;
using ThunderApp.Models;
using ThunderApp.Services;

namespace ThunderApp.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly INwsAlertsService _alerts;
    private readonly IGpsService _gps;
    private readonly IDiskLogService _log;
    private readonly ISettingsService<AlertFilterSettings> _settingsSvc;

    public ObservableCollection<NwsAlert> Alerts { get; } = [];
    public ICollectionView AlertsView { get; }

    [ObservableProperty] private AlertFilterSettings filterSettings;
    [ObservableProperty] private string statusText = "Ready";
    [ObservableProperty] private NwsAlert? selectedAlert;

    // WF-style groups (bound in XAML)
    public ObservableCollection<LifecycleGroupViewModel> LifecycleGroups { get; } = [];

    // Event the map listens to
    public event EventHandler? AlertsChanged;

    public DashboardViewModel(
        INwsAlertsService alerts,
        IGpsService gps,
        IDiskLogService log,
        ISettingsService<AlertFilterSettings> settingsSvc)
    {
        _alerts = alerts;
        _gps = gps;
        _log = log;
        _settingsSvc = settingsSvc;

        AlertsView = CollectionViewSource.GetDefaultView(Alerts);
        AlertsView.Filter = FilterAlert;

        try
        {
            FilterSettings = _settingsSvc.Load();
        }
        catch (Exception ex)
        {
            FilterSettings = new AlertFilterSettings();
            _log.Log("Settings load failed: " + ex);
        }

        BuildLifecycleGroups();

        // Refresh the list + map when:
        // - severity toggles change
        // - near-me settings change
        // - category chip changes
        FilterSettings.PropertyChanged += (_, e) =>
        {
            // keep it cheap: only react to relevant properties
            if (e.PropertyName is nameof(AlertFilterSettings.ShowExtreme)
                or nameof(AlertFilterSettings.ShowSevere)
                or nameof(AlertFilterSettings.ShowModerate)
                or nameof(AlertFilterSettings.ShowMinor)
                or nameof(AlertFilterSettings.ShowUnknown)
                or nameof(AlertFilterSettings.UseNearMe)
                or nameof(AlertFilterSettings.ManualLat)
                or nameof(AlertFilterSettings.ManualLon)
                or nameof(AlertFilterSettings.SelectedCategory)
                or nameof(AlertFilterSettings.DisabledEvents))
            {
                RefreshAlertsView();
                RefreshLifecycleGroupVisibilityFromCategory();
            }
        };

        RefreshLifecycleGroupVisibilityFromCategory();
    }

    private void BuildLifecycleGroups()
    {
        LifecycleGroups.Clear();

        // Create groups in WF-ish order
        var order = new[]
        {
            AlertLifecycle.ShortFusedWarnings,
            AlertLifecycle.LongFusedWarnings,
            AlertLifecycle.Watches,
            AlertLifecycle.Advisories,
            AlertLifecycle.Statements,
            AlertLifecycle.Discussions,
            AlertLifecycle.Outlooks
        };

        foreach (var lc in order)
            LifecycleGroups.Add(new LifecycleGroupViewModel(lc));

        // Populate from catalog
        foreach (var def in AlertCatalog.All)
        {
            var group = LifecycleGroups.First(g => g.Lifecycle == def.Lifecycle);

            bool enabled = FilterSettings.IsEventEnabled(def.EventName);
            var vm = new AlertTypeToggleViewModel(def, enabled);

            // THIS is the missing piece you were seeing:
            // When you flip a toggle, we immediately refresh the view + redraw polygons.
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AlertTypeToggleViewModel.IsEnabled))
                {
                    FilterSettings.SetEventEnabled(vm.EventName, vm.IsEnabled);

                    // Persist optional (you can remove if you only want manual Save)
                    // _settingsSvc.Save(FilterSettings);

                    RefreshAlertsView();
                }
            };

            group.Events.Add(vm);
        }
    }

    private void RefreshLifecycleGroupVisibilityFromCategory()
    {
        // Nothing fancy: we just leave the groups; XAML binds and shows only those with matching category items.
        // If you later want to hide empty lifecycles, we can add a computed property per group.
        OnPropertyChanged(nameof(LifecycleGroups));
    }

    private int _refreshTick;

    private void RefreshAlertsView()
    {
        _refreshTick++;
        _log.Log($"RefreshAlertsView tick={_refreshTick}");

        if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() == false)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                AlertsView?.Refresh();
                AlertsChanged?.Invoke(this, EventArgs.Empty);
            });
            return;
        }

        AlertsView?.Refresh();
        AlertsChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool FilterAlert(object obj)
    {
        if (obj is not NwsAlert a) return false;

        // ---------------- severity ----------------
        string sev = (a.Severity ?? "Unknown").Trim();

        if (sev.Equals("Extreme", StringComparison.OrdinalIgnoreCase) && !FilterSettings.ShowExtreme) return false;
        if (sev.Equals("Severe", StringComparison.OrdinalIgnoreCase) && !FilterSettings.ShowSevere) return false;
        if (sev.Equals("Moderate", StringComparison.OrdinalIgnoreCase) && !FilterSettings.ShowModerate) return false;
        if (sev.Equals("Minor", StringComparison.OrdinalIgnoreCase) && !FilterSettings.ShowMinor) return false;
        if (sev.Equals("Unknown", StringComparison.OrdinalIgnoreCase) && !FilterSettings.ShowUnknown) return false;

        // ---------------- per-event (WF style) ----------------
        string evt = (a.Event ?? "").Trim();
        if (string.IsNullOrWhiteSpace(evt)) return false;

        // If we know this event, it must match selected category to be shown.
        var def = AlertCatalog.All.FirstOrDefault(d => evt.Equals(d.EventName, StringComparison.OrdinalIgnoreCase));

        if (def is not null)
        {
            if (def.Category != FilterSettings.SelectedCategory) return false;
            if (!FilterSettings.IsEventEnabled(def.EventName)) return false;
            return true;
        }

        // Unknown events:
        // Only show them in "Other" category, and allow disabling by name too.
        if (FilterSettings.SelectedCategory != AlertCategory.Other) return false;
        if (!FilterSettings.IsEventEnabled(evt)) return false;

        return true;
    }

    [RelayCommand]
    private void SaveFilters()
    {
        _settingsSvc.Save(FilterSettings);
        _log.Log("Saved alert filters/settings.");
        StatusText = "Saved.";
    }

    [RelayCommand]
    private void SetCategory(string category)
    {
        if (Enum.TryParse<AlertCategory>(category, ignoreCase: true, out var c))
            FilterSettings.SelectedCategory = c;

        RefreshAlertsView();
    }
    
    [RelayCommand]
    internal async Task RefreshAsync()
    {
        try
        {
            StatusText = "Refreshing alerts...";
            _log.Log("Refreshing alerts.");

            IReadOnlyList<NwsAlert> items;

            if (FilterSettings.UseNearMe)
            {
                GeoPoint point = await _gps.GetCurrentAsync(CancellationToken.None)
                                ?? new GeoPoint(FilterSettings.ManualLat, FilterSettings.ManualLon);

                items = await _alerts.GetActiveAlertsForPointAsync(point, CancellationToken.None);
            }
            else
            {
                items = await _alerts.GetActiveAlertsAsync(CancellationToken.None);
            }

            Alerts.Clear();

            foreach (NwsAlert a in items.OrderByDescending(x => x.Effective ?? DateTimeOffset.MinValue))
                Alerts.Add(a);

            RefreshAlertsView();
            StatusText = $"Loaded {Alerts.Count} alerts.";
        }
        catch (Exception ex)
        {
            _log.Log("Refresh failed: " + ex);
            StatusText = "Refresh failed (see log).";
        }
    }
}
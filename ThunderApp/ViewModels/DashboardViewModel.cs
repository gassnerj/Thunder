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

    // Prevent hammering RefreshAlertsView() during bulk enable/disable operations.
    private bool _suppressToggleHandlers;

    // Track dynamically discovered event names so every polygon can be toggled.
    private readonly HashSet<string> _dynamicEventNames = new(StringComparer.OrdinalIgnoreCase);

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

            // When you flip a toggle, refresh the view + redraw polygons.
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AlertTypeToggleViewModel.IsEnabled))
                {
                    FilterSettings.SetEventEnabled(vm.EventName, vm.IsEnabled);
                    if (!_suppressToggleHandlers)
                        RefreshAlertsView();
                }
            };

            group.Events.Add(vm);
        }

        // Apply initial category filter to the toggle list
        RefreshLifecycleGroupVisibilityFromCategory();
    }

    private void EnsureTogglesForLoadedAlerts(IEnumerable<NwsAlert> items)
    {
        // Add missing toggles for event types not present in AlertCatalog.
        // These are the "mystery polygons" the user can't turn off.
        var events = items
            .Select(a => (a.Event ?? "").Trim())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (events.Count == 0) return;

        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in LifecycleGroups)
            foreach (var ev in g.Events)
                existing.Add(ev.EventName);

        bool addedAny = false;

        foreach (var evt in events)
        {
            if (existing.Contains(evt))
                continue;

            if (!_dynamicEventNames.Add(evt))
                continue;

            var lifecycle = AlertClassifier.GetLifecycle(evt);
            var cat = AlertClassifier.GetCategories(evt).FirstOrDefault();
            if (!Enum.IsDefined(typeof(AlertCategory), cat))
                cat = AlertCategory.Other;

            var def = new AlertTypeDefinition(evt, cat, lifecycle);
            var group = LifecycleGroups.FirstOrDefault(g => g.Lifecycle == lifecycle)
                        ?? LifecycleGroups.First(g => g.Lifecycle == AlertLifecycle.LongFusedWarnings);

            bool enabled = FilterSettings.IsEventEnabled(def.EventName);
            var vm = new AlertTypeToggleViewModel(def, enabled);

            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AlertTypeToggleViewModel.IsEnabled))
                {
                    FilterSettings.SetEventEnabled(vm.EventName, vm.IsEnabled);
                    if (!_suppressToggleHandlers)
                        RefreshAlertsView();
                }
            };

            group.Events.Add(vm);
            addedAny = true;
        }

        if (addedAny)
            RefreshLifecycleGroupVisibilityFromCategory();
    }

    private void RefreshLifecycleGroupVisibilityFromCategory()
    {
        foreach (var g in LifecycleGroups)
            g.SetSelectedCategory(FilterSettings.SelectedCategory);
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

        // If we know this event, it must be enabled.
        // IMPORTANT: SelectedCategory is for *organizing the filter UI only*.
        // It must NOT hide alerts from other categories.
        var def = AlertCatalog.All.FirstOrDefault(d => evt.Equals(d.EventName, StringComparison.OrdinalIgnoreCase));

        if (def is not null)
        {
            if (!FilterSettings.IsEventEnabled(def.EventName)) return false;
            return true;
        }

        // Unknown events: allow disabling by name.
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
    private void EnableAllAlertTypes()
    {
        _suppressToggleHandlers = true;
        try
        {
            foreach (var g in LifecycleGroups)
                foreach (var ev in g.Events)
                {
                    FilterSettings.SetEventEnabled(ev.EventName, enabled: true);
                    ev.IsEnabled = true;
                }
        }
        finally
        {
            _suppressToggleHandlers = false;
        }

        RefreshAlertsView();
    }

    [RelayCommand]
    private void DisableAllAlertTypes()
    {
        _suppressToggleHandlers = true;
        try
        {
            foreach (var g in LifecycleGroups)
                foreach (var ev in g.Events)
                {
                    FilterSettings.SetEventEnabled(ev.EventName, enabled: false);
                    ev.IsEnabled = false;
                }
        }
        finally
        {
            _suppressToggleHandlers = false;
        }

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

            // Ensure *every* loaded event type is represented in the filter UI.
            EnsureTogglesForLoadedAlerts(items);

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
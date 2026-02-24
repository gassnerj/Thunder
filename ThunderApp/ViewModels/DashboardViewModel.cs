using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeoJsonWeather;
using ThunderApp.Models;
using ThunderApp.Services;

namespace ThunderApp.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

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

        // Whenever ANY filter property changes → refresh + redraw
        FilterSettings.PropertyChanged += (_,__) => RefreshAlertsView();

        // If you implemented HiddenEventsChanged in your settings
        FilterSettings.HiddenEventsChanged += (_,__) => RefreshAlertsView();
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

        // ---------------- type (by Event string) ----------------
        string evt = (a.Event ?? "").Trim();

        bool isTornado = evt.Contains("Tornado", StringComparison.OrdinalIgnoreCase);
        bool isSevereTs = evt.Contains("Severe Thunderstorm", StringComparison.OrdinalIgnoreCase);
        bool isFlashFlood = evt.Contains("Flash Flood", StringComparison.OrdinalIgnoreCase);
        bool isWinter =
            evt.Contains("Winter", StringComparison.OrdinalIgnoreCase) ||
            evt.Contains("Blizzard", StringComparison.OrdinalIgnoreCase) ||
            evt.Contains("Ice", StringComparison.OrdinalIgnoreCase) ||
            evt.Contains("Snow", StringComparison.OrdinalIgnoreCase) ||
            evt.Contains("Freezing", StringComparison.OrdinalIgnoreCase);

        if (isTornado && !FilterSettings.ShowTornado) return false;
        if (isSevereTs && !FilterSettings.ShowSevereThunderstorm) return false;
        if (isFlashFlood && !FilterSettings.ShowFlashFlood) return false;
        if (isWinter && !FilterSettings.ShowWinter) return false;

        if (isTornado || isSevereTs || isFlashFlood || isWinter)
            return !FilterSettings.HiddenEvents.Contains(evt);

        if (!FilterSettings.ShowOther) return false;

        return !FilterSettings.HiddenEvents.Contains(evt);
    }

    partial void OnFilterSettingsChanged(AlertFilterSettings value)
    {
        RefreshAlertsView();
    }

    [RelayCommand]
    private void SaveFilters()
    {
        _settingsSvc.Save(FilterSettings);
        _log.Log("Saved alert filters/settings.");
        StatusText = "Saved.";
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
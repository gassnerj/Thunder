using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        try
        {
            FilterSettings = _settingsSvc.Load();
        }
        catch (Exception ex)
        {
            FilterSettings = new AlertFilterSettings();
            //StatusText = "Settings load failed (defaults).";
            _log.Log("Settings load failed: " + ex);
        }

        AlertsView = CollectionViewSource.GetDefaultView(Alerts);
        AlertsView.Filter = FilterAlert;
    }

    private bool FilterAlert(object obj)
    {
        if (obj is not NwsAlert a) return false;

        // severity toggle
        string sev = (a.Severity ?? "Unknown").Trim();
        if (sev.Equals("Extreme", StringComparison.OrdinalIgnoreCase) && !FilterSettings.ShowExtreme) return false;
        if (sev.Equals("Severe", StringComparison.OrdinalIgnoreCase) && !FilterSettings.ShowSevere) return false;
        if (sev.Equals("Moderate", StringComparison.OrdinalIgnoreCase) && !FilterSettings.ShowModerate) return false;
        if (sev.Equals("Minor", StringComparison.OrdinalIgnoreCase) && !FilterSettings.ShowMinor) return false;
        if (sev.Equals("Unknown", StringComparison.OrdinalIgnoreCase) && !FilterSettings.ShowUnknown) return false;

        // event hide list
        return !FilterSettings.HiddenEvents.Contains(a.Event);
    }

    partial void OnFilterSettingsChanged(AlertFilterSettings value)
    {
        AlertsView?.Refresh();
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

            AlertsView.Refresh();
            StatusText = $"Loaded {Alerts.Count} alerts.";
        }
        catch (Exception ex)
        {
            _log.Log("Refresh failed: " + ex);
            StatusText = "Refresh failed (see log).";
        }
    }
}
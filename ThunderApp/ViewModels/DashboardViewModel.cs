using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoJsonWeather;
using ThunderApp.Models;
using ThunderApp.Services;
using ThunderApp.Views;

namespace ThunderApp.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly INwsAlertsService _alerts;
    private readonly IGpsService _gps;
    private readonly IDiskLogService _log;
    private readonly ISettingsService<AlertFilterSettings> _settingsSvc;
    private readonly NwsZoneGeometryService _zones;
    private readonly SpcOutlookTextService _spcText;

    private ObservableCollection<NwsAlert> _alertsCollection = [];
    public ObservableCollection<NwsAlert> Alerts
    {
        get => _alertsCollection;
        private set => SetProperty(ref _alertsCollection, value);
    }

    private ICollectionView _alertsView = null!;
    public ICollectionView AlertsView
    {
        get => _alertsView;
        private set => SetProperty(ref _alertsView, value);
    }

    [ObservableProperty] private AlertFilterSettings filterSettings;
    [ObservableProperty] private string statusText = "Ready";
    [ObservableProperty] private string refreshInfo = "";
    [ObservableProperty] private NwsAlert? selectedAlert;

    // In-app toast for new/entering-range critical warnings.
    [ObservableProperty] private bool toastVisible;
    [ObservableProperty] private string toastTitle = "";
    [ObservableProperty] private string toastMessage = "";

    // Single textbox input for manual coordinates, e.g. "35.1394, -92.1053".
    [ObservableProperty] private string manualCoordsText = "";

    // Shows the last clicked map coordinates (single click).
    [ObservableProperty] private string mapClickCoordsText = "";

    // Latest known location (GPS or manual). Used for radius filtering + fast refresh.
    [ObservableProperty] private GeoPoint? currentLocation;

    // SPC outlook text (SWO) for Day 1/2/3.
    [ObservableProperty] private string spcDay1Text = "";
    [ObservableProperty] private string spcDay2Text = "";
    [ObservableProperty] private string spcDay3Text = "";

    // WF-style groups (bound in XAML)
    public ObservableCollection<LifecycleGroupViewModel> LifecycleGroups { get; } = [];

    // Event the map listens to
    public event EventHandler? AlertsChanged;

    // Prevent hammering RefreshAlertsView() during bulk enable/disable operations.
    private bool _suppressToggleHandlers;

    // Track dynamically discovered event names so every polygon can be toggled.
    private readonly HashSet<string> _dynamicEventNames = new(StringComparer.OrdinalIgnoreCase);

    // Cache per-alert bounds for distance filtering.
    private readonly Dictionary<string, GeoBounds> _alertBoundsCache = new(StringComparer.OrdinalIgnoreCase);

    // Cache per-zone bounds for distance filtering (used when alerts only have zone URLs).
    private readonly Dictionary<string, GeoBounds> _zoneBoundsCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _zonePointPending = new(StringComparer.OrdinalIgnoreCase);

    private readonly CancellationTokenSource _autoRefreshCts = new();
    private DateTimeOffset _lastFastTick = DateTimeOffset.MinValue;
    private DateTimeOffset _lastSlowTick = DateTimeOffset.MinValue;

    // Notification state for critical warnings.
    private readonly HashSet<string> _seenCriticalIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _lastInRangeById = new(StringComparer.OrdinalIgnoreCase);
    private DispatcherTimer? _toastTimer;

    public DashboardViewModel(
        INwsAlertsService alerts,
        IGpsService gps,
        IDiskLogService log,
        ISettingsService<AlertFilterSettings> settingsSvc,
        NwsZoneGeometryService zones,
        SpcOutlookTextService spcText)
    {
        _alerts = alerts;
        _gps = gps;
        _log = log;
        _settingsSvc = settingsSvc;
        _zones = zones;
        _spcText = spcText;

        RebuildAlertsView();

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

        // Initialize center from saved manual coordinates (works even without a GPS puck).
        SyncManualCoordsTextFromSettings();
        if (IsUsableManualPoint(FilterSettings.ManualLat, FilterSettings.ManualLon))
            CurrentLocation = new GeoPoint(FilterSettings.ManualLat, FilterSettings.ManualLon);

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
                or nameof(AlertFilterSettings.UseRadiusFilter)
                or nameof(AlertFilterSettings.RadiusMiles)
                or nameof(AlertFilterSettings.SelectedCategory)
                or nameof(AlertFilterSettings.DisabledEvents))
            {
                RefreshAlertsView();
                RefreshLifecycleGroupVisibilityFromCategory();
            }
        };

        RefreshLifecycleGroupVisibilityFromCategory();

        // Start background refresh loops.
        _ = RunAutoRefreshLoopsAsync(_autoRefreshCts.Token);
    }

    private void RebuildAlertsView()
    {
        AlertsView = CollectionViewSource.GetDefaultView(Alerts);
        AlertsView.Filter = FilterAlert;
    }

    partial void OnManualCoordsTextChanged(string value)
    {
        if (_suppressManualCoordsSync) return;

        if (TryParseLatLon(value, out var lat, out var lon))
        {
            _suppressManualCoordsSync = true;
            try
            {
                FilterSettings.ManualLat = lat;
                FilterSettings.ManualLon = lon;
                CurrentLocation = new GeoPoint(lat, lon);
            }
            finally
            {
                _suppressManualCoordsSync = false;
            }

            // Manual coordinate changes should immediately re-apply filters and repaint polygons,
            // without requiring a full network refresh or toggle flip.
            RefreshAlertsView();
        }
    }

    private bool _suppressManualCoordsSync;

    private void SyncManualCoordsTextFromSettings()
    {
        _suppressManualCoordsSync = true;
        try
        {
            ManualCoordsText = $"{FilterSettings.ManualLat:0.##########}, {FilterSettings.ManualLon:0.##########}";
        }
        finally
        {
            _suppressManualCoordsSync = false;
        }
    }

    private static bool IsUsableManualPoint(double lat, double lon)
        => Math.Abs(lat) > 0.0001 && Math.Abs(lon) > 0.0001;

    private static bool TryParseLatLon(string? text, out double lat, out double lon)
    {
        lat = 0;
        lon = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        // Accept: "lat, lon" or "lat lon" or "(lat, lon)"
        var parts = text
            .Replace("(", "")
            .Replace(")", "")
            .Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2) return false;

        if (!double.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out lat))
            return false;
        if (!double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out lon))
            return false;

        if (lat is < -90 or > 90) return false;
        if (lon is < -180 or > 180) return false;
        return true;
    }

    public void SetCurrentLocation(double lat, double lon)
    {
        CurrentLocation = new GeoPoint(lat, lon);
        // Wake the fast loop quickly when we get fresh GPS.
        _lastFastTick = DateTimeOffset.MinValue;
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
        }

        // Unknown events: allow disabling by name.
        if (!FilterSettings.IsEventEnabled(evt)) return false;

        // ---------------- radius ----------------
        if (FilterSettings.UseRadiusFilter)
        {
            // Prefer CurrentLocation (GPS OR manual). Fall back to manual settings.
            GeoPoint center = CurrentLocation ?? new GeoPoint(FilterSettings.ManualLat, FilterSettings.ManualLon);

            // If we still don't have a usable center, show nothing (prevents 0,0 from leaking everything).
            if (Math.Abs(center.Lat) < 0.0001 && Math.Abs(center.Lon) < 0.0001)
                return false;

            var alertBounds = TryGetAlertBounds(a);
            if (alertBounds is null)
            {
                // Strict: if we cannot evaluate range yet, treat as out-of-range.
                // But queue zone bounds resolution so it will appear once we can compute bounds.
                if (a.AffectedZonesUrls is { Count: > 0 })
                    QueueZonePointResolve(a);

                return false;
            }

            double miles = alertBounds.Value.DistanceMilesTo(center);
            if (miles > FilterSettings.RadiusMiles)
                return false;
        }

        return true;
    }

    private GeoBounds? TryGetAlertBounds(NwsAlert a)
    {
        if (!string.IsNullOrWhiteSpace(a.Id) && _alertBoundsCache.TryGetValue(a.Id, out var cached))
            return cached;

        if (string.IsNullOrWhiteSpace(a.GeometryJson))
        {
            // Some alerts ship without explicit geometry and only include zone URLs.
            // If we can resolve the zone geometry, use that.
            if (a.AffectedZonesUrls is { Count: > 0 })
            {
                GeoBounds? combined = null;
                foreach (var zoneUrl in a.AffectedZonesUrls)
                {
                    if (_zoneBoundsCache.TryGetValue(zoneUrl, out var zb))
                        combined = combined is null ? zb : GeoBounds.Union(combined.Value, zb);
                }

                // Queue any missing zones (deduped inside QueueZonePointResolve).
                QueueZonePointResolve(a);

                if (combined is not null)
                    return combined;
            }

            return null;
        }

        if (!TryGetBoundsFromGeoJson(a.GeometryJson, out var b))
            return null;

        if (!string.IsNullOrWhiteSpace(a.Id))
            _alertBoundsCache[a.Id] = b;

        return b;
    }

    private void QueueZonePointResolve(NwsAlert a)
    {
        if (a.AffectedZonesUrls is not { Count: > 0 }) return;

        // Queue all zones referenced by this alert (deduped).
        foreach (var zoneUrl in a.AffectedZonesUrls)
        {
            lock (_zonePointPending)
            {
                if (_zoneBoundsCache.ContainsKey(zoneUrl)) continue;
                if (_zonePointPending.Contains(zoneUrl)) continue;
                _zonePointPending.Add(zoneUrl);
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    string? geom = await _zones.GetGeometryJsonAsync(zoneUrl).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(geom)) return;

                    if (!TryGetBoundsFromGeoJson(geom, out var b)) return;

                    lock (_zonePointPending)
                    {
                        _zoneBoundsCache[zoneUrl] = b;
                        _zonePointPending.Remove(zoneUrl);
                    }

                    // Trigger a refresh so radius filtering can re-evaluate.
                    App.Current.Dispatcher.Invoke(RefreshAlertsView);
                }
                catch
                {
                    lock (_zonePointPending)
                    {
                        _zonePointPending.Remove(zoneUrl);
                    }
                }
            });
        }
    }

    private static bool TryGetBoundsFromGeoJson(string geometryJson, out GeoBounds bounds)
    {
        bounds = default;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(geometryJson);
            var root = doc.RootElement;
            var type = root.GetProperty("type").GetString() ?? "";

            // Handle Point quickly
            if (type.Equals("Point", StringComparison.OrdinalIgnoreCase))
            {
                var c = root.GetProperty("coordinates");
                var lat = c[1].GetDouble();
                var lon = c[0].GetDouble();
                bounds = GeoBounds.FromPoint(lat, lon);
                return true;
            }

            if (type.Equals("Polygon", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("MultiPolygon", StringComparison.OrdinalIgnoreCase))
            {
                if (!root.TryGetProperty("coordinates", out var coords)) return false;
                return TryBoundsFromCoordinates(coords, out bounds);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryBoundsFromCoordinates(System.Text.Json.JsonElement coords, out GeoBounds bounds)
    {
        bounds = default;
        double minLat = double.MaxValue, minLon = double.MaxValue;
        double maxLat = double.MinValue, maxLon = double.MinValue;

        // Iterative walk to avoid recursion limits.
        var stack = new Stack<System.Text.Json.JsonElement>();
        stack.Push(coords);

        while (stack.Count > 0)
        {
            var el = stack.Pop();
            if (el.ValueKind != System.Text.Json.JsonValueKind.Array) continue;

            // A point is an array where the first two elements are numbers.
            if (el.GetArrayLength() >= 2 && el[0].ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                double lon = el[0].GetDouble();
                double lat = el[1].GetDouble();

                if (lat < minLat) minLat = lat;
                if (lat > maxLat) maxLat = lat;
                if (lon < minLon) minLon = lon;
                if (lon > maxLon) maxLon = lon;
                continue;
            }

            foreach (var child in el.EnumerateArray())
                stack.Push(child);
        }

        if (minLat == double.MaxValue) return false;
        bounds = new GeoBounds(minLat, minLon, maxLat, maxLon);
        return true;
    }

    private static double HaversineMiles(double lat1, double lon1, double lat2, double lon2)
        => HaversineMeters(lat1, lon1, lat2, lon2) / 1609.344;

    private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        static double ToRad(double deg) => deg * (Math.PI / 180.0);

        double dLat = ToRad(lat2 - lat1);
        double dLon = ToRad(lon2 - lon1);

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }


    private void UpdateRefreshInfo(string? note = null)
    {
        string fast = _lastFastTick == DateTimeOffset.MinValue ? "--" : _lastFastTick.ToLocalTime().ToString("HH:mm:ss");
        string slow = _lastSlowTick == DateTimeOffset.MinValue ? "--" : _lastSlowTick.ToLocalTime().ToString("HH:mm:ss");
        RefreshInfo = note is null ? $"Fast: {fast}   Slow: {slow}" : $"Fast: {fast}   Slow: {slow}   {note}";
    }

    private async Task RunAutoRefreshLoopsAsync(CancellationToken ct)
    {
        // Defaults tuned for chase use.
        TimeSpan fast = TimeSpan.FromSeconds(30);
        TimeSpan slow = TimeSpan.FromMinutes(5);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;

                // Slow cycle: refresh full set.
                if (now - _lastSlowTick >= slow)
                {
                    _lastSlowTick = now;
                    UpdateRefreshInfo();
                    await RefreshAsync();
                }

                // Fast cycle: if we have a usable center (GPS OR manual), do a point refresh for critical warnings.
                var center = CurrentLocation;
                if (center is not null && IsUsableManualPoint(center.Value.Lat, center.Value.Lon) && now - _lastFastTick >= fast)
                {
                    _lastFastTick = now;
                    UpdateRefreshInfo();
                    await RefreshCriticalGlobalAsync(center.Value, ct);
                }
            }
            catch (Exception ex)
            {
                _log.Log("Auto refresh loop error: " + ex);
            }

            try { await Task.Delay(1000, ct); } catch { }
        }
    }

    private static readonly HashSet<string> _criticalEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "Tornado Warning",
        "Severe Thunderstorm Warning",
        "Flash Flood Warning",
        // Keep the fast loop lean. Add more later if you want.
    };

    private async Task RefreshCriticalGlobalAsync(GeoPoint center, CancellationToken ct)
    {
        IReadOnlyList<NwsAlert> items;
        try
        {
            // Fast loop: pull ALL TOR/SVR/FFW nationwide (still usually small), then filter locally by radius.
            items = await _alerts.GetActiveAlertsByEventsAsync(_criticalEvents, ct);
        }
        catch (Exception ex)
        {
            _log.Log("Fast critical refresh failed: " + ex);
            return;
        }

        var critical = items
            .Where(a => !string.IsNullOrWhiteSpace(a.Event) && _criticalEvents.Contains(a.Event.Trim()))
            .ToList();

        if (critical.Count == 0) { UpdateRefreshInfo("Fast: none"); return; }

        // Determine which critical alerts are currently in-range (or effectively in-range if radius is off).
        // We'll use this both for notifications and for downstream consumers (like vMix graphics).
        var inRangeNow = new List<NwsAlert>();
        foreach (var a in critical)
        {
            if (IsCriticalInRange(a, center))
                inRangeNow.Add(a);
        }

        // Notify only on NEW alerts or alerts that ENTER range as you move.
        var toNotify = new List<NwsAlert>();
        foreach (var a in critical)
        {
            if (string.IsNullOrWhiteSpace(a.Id))
                continue;

            bool nowInRange = IsCriticalInRange(a, center);
            bool had = _lastInRangeById.TryGetValue(a.Id, out var prevInRange);

            bool isNew = !_seenCriticalIds.Contains(a.Id);
            bool enteredRange = had && prevInRange == false && nowInRange == true;

            if ((isNew && nowInRange) || enteredRange)
                toNotify.Add(a);

            _seenCriticalIds.Add(a.Id);
            _lastInRangeById[a.Id] = nowInRange;
        }

        if (toNotify.Count > 0)
            RaiseCriticalNotification(toNotify);

        // Merge into the main list (upsert by Id).
        if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() == false)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => MergeAlerts(critical));
        }
        else
        {
            MergeAlerts(critical);
        }

        UpdateRefreshInfo($"Fast: {inRangeNow.Count} in-range");

        EnsureTogglesForLoadedAlerts(items);
        RefreshAlertsView();
    }

    private bool IsCriticalInRange(NwsAlert a, GeoPoint center)
    {
        // Respect the user's radius setting if enabled.
        if (!FilterSettings.UseRadiusFilter)
            return true;

        if (Math.Abs(center.Lat) < 0.0001 && Math.Abs(center.Lon) < 0.0001)
            return false;

        var b = TryGetAlertBounds(a);
        if (b is null)
        {
            // For critical warnings we can still be strict: if we can't compute a point yet, treat as out-of-range.
            // (Most TOR/SVR/FFW include geometry anyway.)
            if (a.AffectedZonesUrls is { Count: > 0 })
                QueueZonePointResolve(a);

            return false;
        }

        return b.Value.DistanceMilesTo(center) <= FilterSettings.RadiusMiles;
    }

    private void RaiseCriticalNotification(IReadOnlyList<NwsAlert> alerts)
    {
        // Keep this minimal and non-blocking.
        try
        {
            System.Media.SystemSounds.Exclamation.Play();
        }
        catch { }

        var first = alerts[0];
        ToastTitle = $"{first.Event}";
        ToastMessage = first.Headline ?? first.AreaDescription ?? "New warning";
        ToastVisible = true;

        _toastTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        _toastTimer.Stop();
        _toastTimer.Tick -= ToastTimer_Tick;
        _toastTimer.Tick += ToastTimer_Tick;
        _toastTimer.Start();
    }

    private void ToastTimer_Tick(object? sender, EventArgs e)
    {
        _toastTimer?.Stop();
        ToastVisible = false;
    }

    private void MergeAlerts(IReadOnlyList<NwsAlert> incoming)
    {
        foreach (var a in incoming)
        {
            if (string.IsNullOrWhiteSpace(a.Id))
                continue;

            var existing = Alerts.FirstOrDefault(x =>
                string.Equals(x.Id, a.Id, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                Alerts.Insert(0, a);
            }
            else
            {
                // Replace item (simpler than copy-props; keeps ordering stable enough)
                int idx = Alerts.IndexOf(existing);
                if (idx >= 0) Alerts[idx] = a;
            }
        }
    }

    [RelayCommand]
    private void SaveFilters()
    {
        _settingsSvc.Save(FilterSettings);
        _log.Log("Saved alert filters/settings.");
        StatusText = "Saved.";
    }

    [RelayCommand]
    internal async Task RefreshSpcTextAsync()
    {
        try
        {
            StatusText = "Fetching SPC outlook text...";

            string? d1 = await _spcText.GetConvectiveOutlookTextAsync(1).ConfigureAwait(false);
            string? d2 = await _spcText.GetConvectiveOutlookTextAsync(2).ConfigureAwait(false);
            string? d3 = await _spcText.GetConvectiveOutlookTextAsync(3).ConfigureAwait(false);

            // Back to UI thread for property updates.
            if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() == false)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    SpcDay1Text = d1 ?? "(No Day 1 text returned.)";
                    SpcDay2Text = d2 ?? "(No Day 2 text returned.)";
                    SpcDay3Text = d3 ?? "(No Day 3 text returned.)";
                    StatusText = "SPC text updated.";
                });
            }
            else
            {
                SpcDay1Text = d1 ?? "(No Day 1 text returned.)";
                SpcDay2Text = d2 ?? "(No Day 2 text returned.)";
                SpcDay3Text = d3 ?? "(No Day 3 text returned.)";
                StatusText = "SPC text updated.";
            }
        }
        catch (Exception ex)
        {
            _log.Log("SPC text fetch failed: " + ex);
            StatusText = "SPC text fetch failed.";
        }
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

            var sorted = items
                .OrderByDescending(x => x.Effective ?? DateTimeOffset.MinValue)
                .ToList();

            // Swap the collection in one shot to avoid CollectionView
            // current-position issues during DeferRefresh (and to reduce UI freezes).
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                Alerts = new ObservableCollection<NwsAlert>(sorted);
                RebuildAlertsView();

                // Ensure *every* loaded event type is represented in the filter UI.
                EnsureTogglesForLoadedAlerts(sorted);

                RefreshAlertsView();
                StatusText = $"Loaded {Alerts.Count} alerts.";
            });
        }
        catch (Exception ex)
        {
            _log.Log("Refresh failed: " + ex);
            StatusText = "Refresh failed (see log).";
        }
    }
}
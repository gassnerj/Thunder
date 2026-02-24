using System.Collections.ObjectModel;
using ThunderApp.Models;

namespace ThunderApp.ViewModels;

public sealed class AlertCategoryGroupViewModel
{
    public AlertCategory Category { get; }

    // This collection must be STABLE (do not Clear()/rebuild it constantly)
    public ObservableCollection<AlertEventGroupViewModel> Groups { get; } = [];

    private readonly AlertEventGroupViewModel _shortWarn = new(AlertLifecycle.ShortFusedWarnings);
    private readonly AlertEventGroupViewModel _longWarn  = new(AlertLifecycle.LongFusedWarnings);
    private readonly AlertEventGroupViewModel _watch     = new(AlertLifecycle.Watches);
    private readonly AlertEventGroupViewModel _adv       = new(AlertLifecycle.Advisories);
    private readonly AlertEventGroupViewModel _stmt      = new(AlertLifecycle.Statements);
    private readonly AlertEventGroupViewModel _disc      = new(AlertLifecycle.Discussions);
    private readonly AlertEventGroupViewModel _outlook   = new(AlertLifecycle.Outlooks);
    private readonly AlertEventGroupViewModel _other     = new(AlertLifecycle.Other);

    public AlertCategoryGroupViewModel(AlertCategory category)
    {
        Category = category;

        // Fixed order. Always present. UI never collapses / jumps.
        Groups.Add(_shortWarn);
        Groups.Add(_longWarn);
        Groups.Add(_watch);
        Groups.Add(_adv);
        Groups.Add(_stmt);
        Groups.Add(_disc);
        Groups.Add(_outlook);
        Groups.Add(_other);
    }

    // If you want to hide empty sections visually, do it with a Visibility converter in XAML,
    // NOT by removing items from Groups.
    public bool HasAnyEvents =>
        _shortWarn.Events.Count +
        _longWarn.Events.Count +
        _watch.Events.Count +
        _adv.Events.Count +
        _stmt.Events.Count +
        _disc.Events.Count +
        _outlook.Events.Count +
        _other.Events.Count > 0;

    public void AddEvent(AlertFilterSettings settings, AlertLifecycle life, string eventName)
    {
        var item = new AlertEventToggleItem(settings, eventName);

        switch (life)
        {
            case AlertLifecycle.ShortFusedWarnings: _shortWarn.Events.Add(item); break;
            case AlertLifecycle.LongFusedWarnings:  _longWarn.Events.Add(item); break;
            case AlertLifecycle.Watches:             _watch.Events.Add(item); break;
            case AlertLifecycle.Advisories:          _adv.Events.Add(item); break;
            case AlertLifecycle.Statements:         _stmt.Events.Add(item); break;
            case AlertLifecycle.Discussions:        _disc.Events.Add(item); break;
            case AlertLifecycle.Outlooks:           _outlook.Events.Add(item); break;
            default:                               _other.Events.Add(item); break;
        }
    }
}
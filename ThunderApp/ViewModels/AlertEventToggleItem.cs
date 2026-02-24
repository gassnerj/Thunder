using System;
using CommunityToolkit.Mvvm.ComponentModel;
using ThunderApp.Models;

namespace ThunderApp.ViewModels;

public partial class AlertEventToggleItem(AlertFilterSettings settings, string eventName) : ObservableObject
{
    public string EventName { get; } = eventName;

    public bool IsEnabled
    {
        get => !settings.HiddenEvents.Contains(EventName);
        set
        {
            if (value) settings.UnhideEvent(EventName);
            else settings.HideEvent(EventName);

            OnPropertyChanged();
        }
    }
}
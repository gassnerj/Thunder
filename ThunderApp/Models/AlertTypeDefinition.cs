namespace ThunderApp.Models;

/// <summary>
/// Defines a single NWS alert "event" and how it should be grouped in the UI.
/// </summary>
public sealed record AlertTypeDefinition(
    string EventName,
    AlertCategory Category,
    AlertLifecycle Lifecycle
);

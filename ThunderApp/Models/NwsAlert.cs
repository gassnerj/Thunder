using System;
using System.Collections.Generic;

namespace ThunderApp.Models;

public sealed record NwsAlert(
    string Id,
    string Event,
    string Headline,
    string Severity,
    string Urgency,
    DateTimeOffset? Effective,
    DateTimeOffset? Expires,
    DateTimeOffset? Ends,
    DateTimeOffset? Onset,
    string AreaDescription,
    string? SenderName,
    string? Description,
    string? Instruction,
    string? GeometryJson,
    IReadOnlyList<string>? AffectedZonesUrls   // <-- NEW
)
{
    public NwsAlert() : this(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        null,
        null,
        null,
        null,
        string.Empty,
        null,
        null,
        null,
        null,
        null
    )
    {
    }
}
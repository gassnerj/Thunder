using System;

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
    string? Instruction
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
        null
    )
    {
    }
}
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
    string AreaDesc,
    string? SenderName
);
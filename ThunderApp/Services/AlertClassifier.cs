using System;
using System.Collections.Generic;
using ThunderApp.Models;

namespace ThunderApp.Services;

public static class AlertClassifier
{
public static AlertLifecycle GetLifecycle(string? eventName)
{
    string e = (eventName ?? "").Trim();
    if (e.Length == 0) return AlertLifecycle.Other;

    // ----- WF-style buckets -----

    // Discussions
    if (e.Contains("Discussion", StringComparison.OrdinalIgnoreCase) ||
        e.Contains("Mesoscale", StringComparison.OrdinalIgnoreCase))
        return AlertLifecycle.Discussions;

    // Outlooks
    if (e.Contains("Outlook", StringComparison.OrdinalIgnoreCase))
        return AlertLifecycle.Outlooks;

    // Watches
    if (e.Contains("Watch", StringComparison.OrdinalIgnoreCase))
        return AlertLifecycle.Watches;

    // Advisories
    if (e.Contains("Advisory", StringComparison.OrdinalIgnoreCase))
        return AlertLifecycle.Advisories;

    // Statements (NWS uses several "Statement"/"Message"/"Alert" flavors)
    if (e.Contains("Statement", StringComparison.OrdinalIgnoreCase) ||
        e.Contains("Weather Statement", StringComparison.OrdinalIgnoreCase) ||
        e.Contains("Marine Weather Statement", StringComparison.OrdinalIgnoreCase) ||
        e.Contains("Hydrologic Outlook", StringComparison.OrdinalIgnoreCase) ||
        e.Contains("Alert", StringComparison.OrdinalIgnoreCase) ||
        e.Contains("Message", StringComparison.OrdinalIgnoreCase))
        return AlertLifecycle.Statements;

    // Warnings -> Short vs Long fused
    if (e.Contains("Warning", StringComparison.OrdinalIgnoreCase))
    {
        // long-fused warnings (big-area, long duration)
        if (ContainsAny(e,
            "Blizzard Warning",
            "Winter Storm Warning",
            "Ice Storm Warning",
            "Snow Storm Warning",
            "Lake Effect Snow Warning",
            "Storm Warning",              // NOTE: marine “Storm Warning” is a warning; keep it long-fused
            "Hurricane Force Wind Warning",
            "Tropical Storm Warning",
            "High Wind Warning",
            "Extreme Wind Warning",
            "Dust Storm Warning",
            "Red Flag Warning",
            "Fire Warning",
            "Gale Warning",
            "Freeze Warning",
            "Hard Freeze Warning",
            "Wind Chill Warning",
            "Excessive Heat Warning",
            "Flood Warning",
            "Coastal Flood Warning",
            "River Flood Warning",
            "Areal Flood Warning"))
            return AlertLifecycle.LongFusedWarnings;

        // short-fused warnings (rapid onset / “take action now”)
        if (ContainsAny(e,
            "Tornado Warning",
            "Severe Thunderstorm Warning",
            "Flash Flood Warning",
            "Snow Squall Warning",
            "Special Marine Warning"))
            return AlertLifecycle.ShortFusedWarnings;

        // default for warnings you haven’t explicitly classified yet
        return AlertLifecycle.LongFusedWarnings;
    }

    return AlertLifecycle.Other;
}

    public static IReadOnlyList<AlertCategory> GetCategories(string? eventName)
    {
        string e = (eventName ?? "").Trim();
        if (e.Length == 0) return [AlertCategory.Other];

        // Multi-tagging: add as many as you want.
        var cats = new HashSet<AlertCategory>();

        // ---- Short-Fused (mostly warnings) ----
        if (IsAny(e,
            "Tornado Warning",
            "Severe Thunderstorm Warning",
            "Flash Flood Warning",
            "Snow Squall Warning",
            "Special Marine Warning",
            "Extreme Wind Warning"))
            cats.Add(AlertCategory.);

        // ---- Long-Fused (mostly watches) ----
        if (IsAny(e,
            "Tornado Watch",
            "Severe Thunderstorm Watch",
            "Flood Watch",
            "Fire Weather Watch",
            "Winter Storm Watch"))
            cats.Add(AlertCategory.LongFused);

        // ---- Winter ----
        if (ContainsAny(e, "Winter", "Blizzard", "Ice Storm", "Snow Squall", "Freezing", "Sleet"))
            cats.Add(AlertCategory.Winter);

        // ---- Fire / Air Quality ----
        if (ContainsAny(e, "Red Flag", "Fire Weather", "Smoke", "Air Quality"))
            cats.Add(AlertCategory.Fire);

        // ---- Wind / Dust ----
        if (ContainsAny(e, "Wind", "Dust Storm", "Blowing Dust"))
            cats.Add(AlertCategory.Wind);

        // ---- Hydrology ----
        if (ContainsAny(e, "Flood", "Hydrologic", "River"))
            cats.Add(AlertCategory.Hydrology);

        // ---- Heat ----
        if (ContainsAny(e, "Heat", "Excessive Heat"))
            cats.Add(AlertCategory.Heat);

        // ---- Fog ----
        if (ContainsAny(e, "Fog"))
            cats.Add(AlertCategory.Fog);

        // ---- Marine ----
        if (ContainsAny(e, "Marine", "Gale", "Storm Warning", "Small Craft", "Hurricane Force"))
            cats.Add(AlertCategory.Marine);

        if (cats.Count == 0) cats.Add(AlertCategory.Other);
        return new List<AlertCategory>(cats);
    }

    private static bool IsAny(string e, params string[] exact)
    {
        foreach (string s in exact)
            if (string.Equals(e, s, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static bool ContainsAny(string e, params string[] parts)
    {
        foreach (string p in parts)
            if (e.Contains(p, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
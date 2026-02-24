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
                "Storm Warning",              // marine “Storm Warning”
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

            return AlertLifecycle.LongFusedWarnings;
        }

        return AlertLifecycle.Other;
    }

    /// <summary>
    /// Broad category chips (Severe/Winter/Fire/etc). This is intentionally coarse.
    /// You can always expand AlertCatalog for exact, per-event mapping.
    /// </summary>
    public static IReadOnlyList<AlertCategory> GetCategories(string? eventName)
    {
        string e = (eventName ?? "").Trim();
        if (e.Length == 0) return [AlertCategory.Other];

        var cats = new HashSet<AlertCategory>();

        // ---- Severe convective / immediate danger ----
        if (ContainsAny(e,
            "Tornado", "Severe Thunderstorm", "Flash Flood",
            "Special Marine Warning", "Extreme Wind", "Hail", "Lightning"))
            cats.Add(AlertCategory.Severe);

        // ---- Winter ----
        if (ContainsAny(e, "Winter", "Blizzard", "Ice", "Snow", "Sleet", "Freezing", "Wind Chill", "Freeze"))
            cats.Add(AlertCategory.Winter);

        // ---- Fire / smoke / air quality ----
        if (ContainsAny(e, "Red Flag", "Fire Weather", "Smoke", "Air Quality"))
            cats.Add(AlertCategory.Fire);

        // ---- Wind / dust ----
        if (ContainsAny(e, "High Wind", "Wind Advisory", "Dust", "Blowing Dust", "Gust"))
            cats.Add(AlertCategory.Wind);

        // ---- Hydrology ----
        if (ContainsAny(e, "Flood", "Hydrologic", "River", "Coastal Flood"))
            cats.Add(AlertCategory.Hydrology);

        // ---- Heat ----
        if (ContainsAny(e, "Heat", "Excessive Heat"))
            cats.Add(AlertCategory.Heat);

        // ---- Fog ----
        if (ContainsAny(e, "Fog"))
            cats.Add(AlertCategory.Fog);

        // ---- Marine ----
        if (ContainsAny(e, "Marine", "Gale", "Small Craft", "Hazardous Seas", "Hurricane Force", "Storm Warning"))
            cats.Add(AlertCategory.Marine);

        // ---- Tropical ----
        if (ContainsAny(e, "Tropical", "Hurricane", "Typhoon", "Storm Surge"))
            cats.Add(AlertCategory.Tropical);

        if (cats.Count == 0) cats.Add(AlertCategory.Other);
        return new List<AlertCategory>(cats);
    }

    private static bool ContainsAny(string e, params string[] parts)
    {
        foreach (string p in parts)
            if (e.Contains(p, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}

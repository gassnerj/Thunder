using System.Collections.Generic;
using ThunderApp.Models;

namespace ThunderApp.Services;

public static class AlertCatalog
{
    // IMPORTANT:
    // EventName values should match NWS "event" field exactly (case-insensitive compare is used).
    // Add/remove freely over time.
    public static readonly IReadOnlyList<AlertTypeDefinition> All = new List<AlertTypeDefinition>
    {
        // ===================== SEVERE =====================
        new("Tornado Warning", AlertCategory.Severe, AlertLifecycle.ShortFusedWarnings),
        new("Severe Thunderstorm Warning", AlertCategory.Severe, AlertLifecycle.ShortFusedWarnings),
        new("Flash Flood Warning", AlertCategory.Severe, AlertLifecycle.ShortFusedWarnings),
        new("Special Marine Warning", AlertCategory.Severe, AlertLifecycle.ShortFusedWarnings),
        new("Extreme Wind Warning", AlertCategory.Severe, AlertLifecycle.ShortFusedWarnings),

        new("Tornado Watch", AlertCategory.Severe, AlertLifecycle.Watches),
        new("Severe Thunderstorm Watch", AlertCategory.Severe, AlertLifecycle.Watches),
        new("Flash Flood Watch", AlertCategory.Severe, AlertLifecycle.Watches),

        new("Special Weather Statement", AlertCategory.Severe, AlertLifecycle.Statements),
        new("Marine Weather Statement", AlertCategory.Severe, AlertLifecycle.Statements),

        new("Mesoscale Discussion", AlertCategory.Severe, AlertLifecycle.Discussions),

        // ===================== FIRE =====================
        new("Red Flag Warning", AlertCategory.Fire, AlertLifecycle.LongFusedWarnings),
        new("Fire Weather Watch", AlertCategory.Fire, AlertLifecycle.Watches),
        new("Dense Smoke Advisory", AlertCategory.Fire, AlertLifecycle.Advisories),
        new("Air Quality Alert", AlertCategory.Fire, AlertLifecycle.Statements),

        // ===================== WINTER =====================
        new("Winter Storm Warning", AlertCategory.Winter, AlertLifecycle.LongFusedWarnings),
        new("Blizzard Warning", AlertCategory.Winter, AlertLifecycle.LongFusedWarnings),
        new("Ice Storm Warning", AlertCategory.Winter, AlertLifecycle.LongFusedWarnings),
        new("Winter Weather Advisory", AlertCategory.Winter, AlertLifecycle.Advisories),
        new("Snow Squall Warning", AlertCategory.Winter, AlertLifecycle.ShortFusedWarnings),
        new("Wind Chill Warning", AlertCategory.Winter, AlertLifecycle.LongFusedWarnings),
        new("Wind Chill Advisory", AlertCategory.Winter, AlertLifecycle.Advisories),
        new("Freeze Warning", AlertCategory.Winter, AlertLifecycle.LongFusedWarnings),

        // ===================== HYDROLOGY =====================
        new("Flood Warning", AlertCategory.Hydrology, AlertLifecycle.LongFusedWarnings),
        new("Flood Watch", AlertCategory.Hydrology, AlertLifecycle.Watches),
        new("Flood Advisory", AlertCategory.Hydrology, AlertLifecycle.Advisories),

        // ===================== WIND =====================
        new("High Wind Warning", AlertCategory.Wind, AlertLifecycle.LongFusedWarnings),
        new("Wind Advisory", AlertCategory.Wind, AlertLifecycle.Advisories),
        new("Dust Storm Warning", AlertCategory.Wind, AlertLifecycle.ShortFusedWarnings),

        // ===================== MARINE =====================
        new("Gale Warning", AlertCategory.Marine, AlertLifecycle.LongFusedWarnings),
        new("Small Craft Advisory", AlertCategory.Marine, AlertLifecycle.Advisories),
        new("Hazardous Seas Warning", AlertCategory.Marine, AlertLifecycle.LongFusedWarnings),

        // ===================== HEAT =====================
        new("Excessive Heat Warning", AlertCategory.Heat, AlertLifecycle.LongFusedWarnings),
        new("Heat Advisory", AlertCategory.Heat, AlertLifecycle.Advisories),

        // ===================== FOG =====================
        new("Dense Fog Advisory", AlertCategory.Fog, AlertLifecycle.Advisories),

        // ===================== TROPICAL =====================
        new("Tropical Storm Warning", AlertCategory.Tropical, AlertLifecycle.LongFusedWarnings),
        new("Hurricane Warning", AlertCategory.Tropical, AlertLifecycle.LongFusedWarnings),

        // ===================== OTHER =====================
        new("Civil Danger Warning", AlertCategory.Other, AlertLifecycle.LongFusedWarnings),
    };
}
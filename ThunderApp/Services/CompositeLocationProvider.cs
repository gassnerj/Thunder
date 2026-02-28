using System;
using System.Collections.Generic;
using ThunderApp.Models;

namespace ThunderApp.Services;

public interface ILocationSource
{
    string SourceName { get; }
    bool TryGetLocation(out GeoPoint location);
}

public sealed class DelegateLocationSource : ILocationSource
{
    private readonly Func<(bool ok, GeoPoint point)> _resolver;

    public DelegateLocationSource(string sourceName, Func<(bool ok, GeoPoint point)> resolver)
    {
        SourceName = sourceName;
        _resolver = resolver;
    }

    public string SourceName { get; }

    public bool TryGetLocation(out GeoPoint location)
    {
        var resolved = _resolver();
        location = resolved.point;
        return resolved.ok;
    }
}

public readonly record struct LocationReading(GeoPoint Point, string Source);

public sealed class CompositeLocationProvider
{
    private readonly IReadOnlyList<ILocationSource> _sources;

    public CompositeLocationProvider(params ILocationSource[] sources)
    {
        _sources = sources ?? Array.Empty<ILocationSource>();
    }

    public bool TryGetCurrent(out LocationReading reading)
    {
        foreach (var source in _sources)
        {
            if (source.TryGetLocation(out var point))
            {
                reading = new LocationReading(point, source.SourceName);
                return true;
            }
        }

        reading = default;
        return false;
    }
}

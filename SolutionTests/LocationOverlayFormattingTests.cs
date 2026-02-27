using ThunderApp.Models;
using ThunderApp.Services;

namespace SolutionTests;

public class LocationOverlayFormattingTests
{
    [Theory]
    [InlineData(0, "N")]
    [InlineData(44, "NE")]
    [InlineData(90, "E")]
    [InlineData(225, "SW")]
    [InlineData(315, "NW")]
    public void BearingToCompass_MapsExpected(double bearing, string expected)
    {
        Assert.Equal(expected, LocationOverlayFormatting.BearingToCompass(bearing));
    }

    [Theory]
    [InlineData("United States Highway 287", "US-287")]
    [InlineData("Interstate 40", "I-40")]
    [InlineData("State Highway 6", "SH-6")]
    [InlineData("Farm to Market Road 1729", "FM 1729")]
    public void NormalizeRoadName_MapsExpected(string input, string expected)
    {
        Assert.Equal(expected, LocationOverlayFormatting.NormalizeRoadName(input));
    }

    [Fact]
    public void CacheKey_RoundsTo4Decimals()
    {
        var key = LocationOverlayFormatting.CacheKey(33.95624, -98.67034);
        Assert.Equal("33.9562:-98.6703", key);
    }

    [Fact]
    public void ComposeSnapshot_UsesRoadPreferredFormat()
    {
        var gps = new GeoPoint(33.9562, -98.6703);
        var geo = new ReverseGeocodeCoordinator.GeocodeResult("US-287", "Wichita Falls", "TX", 33.95, -98.66, "Mapbox", 21);

        var s = LocationOverlayFormatting.ComposeSnapshot(gps, geo, geo.PlacePoint, geo.Source);

        Assert.Equal("US-287 near Wichita Falls, TX", s.LocLine);
        Assert.Contains("•", s.LocDetail);
    }

    [Fact]
    public void ComposeSnapshot_FallbacksToNearCityWhenRoadMissing()
    {
        var gps = new GeoPoint(33.9562, -98.6703);
        var geo = new ReverseGeocodeCoordinator.GeocodeResult("", "Wichita Falls", "TX", 33.92, -98.62, "Nominatim", 44);

        var s = LocationOverlayFormatting.ComposeSnapshot(gps, geo, geo.PlacePoint, geo.Source);

        Assert.StartsWith("Near Wichita Falls, TX", s.LocLine);
    }

    [Fact]
    public void ComposeSnapshot_WorstCaseLatLon()
    {
        var gps = new GeoPoint(33.9562, -98.6703);
        var s = LocationOverlayFormatting.ComposeSnapshot(gps, null, null, "GPS");

        Assert.Equal("33.9562, -98.6703", s.LocLine);
    }
}

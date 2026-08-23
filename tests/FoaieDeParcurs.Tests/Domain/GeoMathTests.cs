using FoaieDeParcurs.Core.Domain;

namespace FoaieDeParcurs.Tests.Domain;

public sealed class GeoMathTests
{
    [Fact]
    public void HaversineDistanceMeters_MatchesKnownDistance_BucurestiToClujNapoca()
    {
        // Known great-circle distance is ~327 km.
        var distance = GeoMath.HaversineDistanceMeters(44.4268, 26.1025, 46.7712, 23.6236);

        Assert.InRange(distance, 320_000, 335_000);
    }

    [Fact]
    public void HaversineDistanceMeters_IsZero_ForTheSamePoint()
    {
        var distance = GeoMath.HaversineDistanceMeters(44.4268, 26.1025, 44.4268, 26.1025);

        Assert.Equal(0, distance, precision: 6);
    }

    [Fact]
    public void Simplify_KeepsFirstAndLastPoint_EvenWhenEverythingElseIsRemoved()
    {
        var now = DateTimeOffset.UtcNow;
        var points = new List<(double, double, DateTimeOffset)>
        {
            (44.0, 26.0, now),
            (44.0001, 26.0001, now.AddSeconds(10)),
            (44.0002, 26.0002, now.AddSeconds(20)),
            (44.1, 26.1, now.AddSeconds(30))
        };

        var simplified = GeoMath.Simplify(points, toleranceMeters: 1000);

        Assert.Equal(points[0], simplified[0]);
        Assert.Equal(points[^1], simplified[^1]);
    }

    [Fact]
    public void Simplify_KeepsAPointThatDeviatesSignificantlyFromTheLine()
    {
        var now = DateTimeOffset.UtcNow;
        // A sharp detour well off the direct line between start and end.
        var points = new List<(double, double, DateTimeOffset)>
        {
            (44.0, 26.0, now),
            (44.5, 26.0, now.AddMinutes(1)), // ~55km east detour
            (44.0, 26.2, now.AddMinutes(2))
        };

        var simplified = GeoMath.Simplify(points, toleranceMeters: 50);

        Assert.Equal(3, simplified.Count);
    }
}

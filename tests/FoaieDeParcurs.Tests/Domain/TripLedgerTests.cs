using FoaieDeParcurs.Core.Domain;
using FoaieDeParcurs.Core.Entities;

namespace FoaieDeParcurs.Tests.Domain;

public sealed class TripLedgerTests
{
    // Real coordinates so the offline gazetteer fallback resolves to sensible names.
    private static readonly (double Lat, double Lng) DepotX = (44.4268, 26.1025); // Bucuresti
    private static readonly (double Lat, double Lng) ClujNapoca = (46.7712, 23.6236);
    private static readonly (double Lat, double Lng) Brasov = (45.6427, 25.5887);

    private readonly RomanianCityGazetteer _gazetteer = new();

    /// <summary>Synthetic straight-line drive: one point every 10s at ~80 km/h.</summary>
    private static List<GpsRawPoint> Drive(
        (double Lat, double Lng) from,
        (double Lat, double Lng) to,
        DateTimeOffset start,
        int pointCount = 30)
    {
        var points = new List<GpsRawPoint>();
        for (var i = 0; i < pointCount; i++)
        {
            var t = (double)i / (pointCount - 1);
            points.Add(new GpsRawPoint
            {
                Latitude = from.Lat + (to.Lat - from.Lat) * t,
                Longitude = from.Lng + (to.Lng - from.Lng) * t,
                Timestamp = start.AddSeconds(10 * i),
                Speed = 22.2, // ~80 km/h
                Accuracy = 5
            });
        }

        return points;
    }

    /// <summary>Synthetic parked cluster: tiny GPS jitter around one spot for the given duration.</summary>
    private static List<GpsRawPoint> Park((double Lat, double Lng) at, DateTimeOffset start, TimeSpan duration)
    {
        var points = new List<GpsRawPoint>();
        var elapsed = TimeSpan.Zero;
        var i = 0;
        while (elapsed <= duration)
        {
            // ~5m jitter, well inside the default 50m stop radius.
            var jitter = 0.00003 * (i % 2 == 0 ? 1 : -1);
            points.Add(new GpsRawPoint
            {
                Latitude = at.Lat + jitter,
                Longitude = at.Lng + jitter,
                Timestamp = start + elapsed,
                Speed = 0,
                Accuracy = 5
            });
            elapsed += TimeSpan.FromMinutes(1);
            i++;
        }

        return points;
    }

    [Fact]
    public void DeriveRouteSegments_SplitsACleanMultiSegmentTrip_AtAKnownLocationStop()
    {
        var start = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);

        var leg1 = Drive(DepotX, ClujNapoca, start);
        var stop = Park(ClujNapoca, leg1[^1].Timestamp.AddSeconds(10), TimeSpan.FromMinutes(6));
        var leg2 = Drive(ClujNapoca, DepotX, stop[^1].Timestamp.AddSeconds(10));

        var points = leg1.Concat(stop).Concat(leg2).ToList();

        var knownLocations = new List<KnownLocation>
        {
            new() { Name = "Depot X", Latitude = DepotX.Lat, Longitude = DepotX.Lng, RadiusMeters = 150, Type = KnownLocationType.Work },
            new() { Name = "Client Site Cluj", Latitude = ClujNapoca.Lat, Longitude = ClujNapoca.Lng, RadiusMeters = 150, Type = KnownLocationType.Custom }
        };

        var segments = TripLedger.DeriveRouteSegments(points, knownLocations, _gazetteer, startFillUpId: null, endFillUpId: 1);

        Assert.Equal(2, segments.Count);

        Assert.Equal("Depot X", segments[0].StartLocationName);
        Assert.Equal("Client Site Cluj", segments[0].EndLocationName);
        Assert.True(segments[0].DistanceKm > 0);

        Assert.Equal("Client Site Cluj", segments[1].StartLocationName);
        Assert.Equal("Depot X", segments[1].EndLocationName);
        Assert.True(segments[1].DistanceKm > 0);

        // Every segment carries the same fill-up boundary passed in for this derivation call.
        Assert.All(segments, s => Assert.Null(s.StartFillUpId));
        Assert.All(segments, s => Assert.Equal(1, s.EndFillUpId));
    }

    [Fact]
    public void DeriveRouteSegments_BridgesASignalGap_WithoutSplittingTheSegment()
    {
        var start = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);
        var points = Drive(DepotX, Brasov, start, pointCount: 20);

        // Simulate a tunnel: remove a run of points in the middle so there's a multi-minute
        // gap in time, but the points on either side are far apart in space (not a stop).
        var withGap = points.Take(8).Concat(points.Skip(14)).ToList();

        var segments = TripLedger.DeriveRouteSegments(withGap, [], _gazetteer, startFillUpId: 1, endFillUpId: 2);

        var segment = Assert.Single(segments);
        Assert.True(segment.DistanceKm > 0);
        // The gap must not have been mistaken for a stop-and-resume (which would produce 2 segments).
    }

    [Fact]
    public void DeriveRouteSegments_SplitsAtAStop_EvenWhenNotNearAnyKnownLocation()
    {
        var start = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);
        var midpoint = ((DepotX.Lat + Brasov.Lat) / 2, (DepotX.Lng + Brasov.Lng) / 2);

        var leg1 = Drive(DepotX, midpoint, start, pointCount: 15);
        var stop = Park(midpoint, leg1[^1].Timestamp.AddSeconds(10), TimeSpan.FromMinutes(7));
        var leg2 = Drive(midpoint, Brasov, stop[^1].Timestamp.AddSeconds(10), pointCount: 15);

        var points = leg1.Concat(stop).Concat(leg2).ToList();

        // No Known Locations at all — every name must come from the offline gazetteer fallback.
        var segments = TripLedger.DeriveRouteSegments(points, [], _gazetteer, startFillUpId: 2, endFillUpId: 3);

        Assert.Equal(2, segments.Count);
        Assert.All(segments, s => Assert.False(string.IsNullOrWhiteSpace(s.StartLocationName)));
        Assert.All(segments, s => Assert.False(string.IsNullOrWhiteSpace(s.EndLocationName)));
        Assert.Equal(segments[0].EndLocationName, segments[1].StartLocationName);
    }

    [Fact]
    public void DeriveRouteSegments_NamesEndpointsFromTheGazetteer_WhenNoKnownLocationIsNearby()
    {
        var start = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);
        var points = Drive(DepotX, Brasov, start);

        var segments = TripLedger.DeriveRouteSegments(points, [], _gazetteer, startFillUpId: 5, endFillUpId: 6);

        var segment = Assert.Single(segments);
        Assert.Equal("Bucuresti", segment.StartLocationName);
        Assert.Equal("Brasov", segment.EndLocationName);
    }

    [Fact]
    public void DeriveRouteSegments_ReturnsNoSegments_WhenTheTrailIsTooShortToBeMeaningful()
    {
        var start = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);
        // Two points a few meters apart — GPS jitter while parked, not an actual trip.
        var points = new List<GpsRawPoint>
        {
            new() { Latitude = DepotX.Lat, Longitude = DepotX.Lng, Timestamp = start },
            new() { Latitude = DepotX.Lat + 0.00002, Longitude = DepotX.Lng, Timestamp = start.AddSeconds(30) }
        };

        var segments = TripLedger.DeriveRouteSegments(points, [], _gazetteer, startFillUpId: 1, endFillUpId: 2);

        Assert.Empty(segments);
    }

    [Fact]
    public void DeriveRouteSegments_SimplifiesThePolyline_SoItIsNotOneEncodedPointPerRawPoint()
    {
        var start = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);
        // A perfectly straight line simplifies to just its two endpoints.
        var points = Drive(DepotX, Brasov, start, pointCount: 50);

        var segments = TripLedger.DeriveRouteSegments(points, [], _gazetteer, startFillUpId: 1, endFillUpId: 2);

        var segment = Assert.Single(segments);
        Assert.NotNull(segment.PolylineEncoded);
        var keptPointCount = segment.PolylineEncoded!.Split(';').Length;
        Assert.True(keptPointCount < points.Count, "Simplification should reduce a straight line to far fewer points.");
    }
}

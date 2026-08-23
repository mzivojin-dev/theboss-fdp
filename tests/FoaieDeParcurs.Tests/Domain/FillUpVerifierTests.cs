using FoaieDeParcurs.Core.Domain;
using FoaieDeParcurs.Core.Entities;

namespace FoaieDeParcurs.Tests.Domain;

public sealed class FillUpVerifierTests
{
    private static readonly (double Lat, double Lng) DepotX = (44.4268, 26.1025);
    private static readonly (double Lat, double Lng) ClujNapoca = (46.7712, 23.6236);
    private static readonly (double Lat, double Lng) Brasov = (45.6427, 25.5887);

    private static FillUp FillUpAt((double Lat, double Lng) station, DateTimeOffset timestamp) => new()
    {
        Timestamp = timestamp,
        StationLatitude = station.Lat,
        StationLongitude = station.Lng,
        LitersFilled = 40,
        AmountPaid = 300,
        CreatedAt = timestamp
    };

    private static RouteSegment SegmentBetween(
        (double Lat, double Lng) start, (double Lat, double Lng) end, DateTimeOffset startTime, DateTimeOffset endTime) => new()
    {
        StartLocationName = "Start",
        StartLatitude = start.Lat,
        StartLongitude = start.Lng,
        StartTimestamp = startTime,
        EndLocationName = "End",
        EndLatitude = end.Lat,
        EndLongitude = end.Lng,
        EndTimestamp = endTime,
        DistanceKm = 42.3
    };

    [Fact]
    public void Verify_Passes_WhenSegmentsAreWellFormedAndTheChainIsContinuous()
    {
        var t0 = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);
        var previousFillUp = FillUpAt(DepotX, t0.AddHours(-3));
        var fillUp = FillUpAt(ClujNapoca, t0);

        var segments = new List<RouteSegment>
        {
            SegmentBetween(DepotX, Brasov, t0.AddHours(-2), t0.AddHours(-1)),
            SegmentBetween(Brasov, ClujNapoca, t0.AddMinutes(-50), t0)
        };

        var result = FillUpVerifier.Verify(fillUp, segments, previousFillUp);

        Assert.True(result.IsVerified);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Verify_Passes_WhenThereAreNoSegmentsAndNoPreviousFillUp()
    {
        var fillUp = FillUpAt(DepotX, DateTimeOffset.UtcNow);

        var result = FillUpVerifier.Verify(fillUp, [], previousFillUp: null);

        Assert.True(result.IsVerified);
    }

    [Fact]
    public void Verify_Fails_WhenASegmentHasNonPositiveDistance()
    {
        var t0 = DateTimeOffset.UtcNow;
        var fillUp = FillUpAt(DepotX, t0);
        var segment = SegmentBetween(DepotX, DepotX, t0.AddHours(-1), t0);
        segment.DistanceKm = 0;

        var result = FillUpVerifier.Verify(fillUp, [segment], previousFillUp: null);

        Assert.False(result.IsVerified);
        Assert.Contains(result.Issues, i => i.Contains("distance", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Verify_Fails_WhenASegmentEndsBeforeItStarts()
    {
        var t0 = DateTimeOffset.UtcNow;
        var fillUp = FillUpAt(DepotX, t0);
        var segment = SegmentBetween(DepotX, ClujNapoca, t0, t0.AddHours(-1));

        var result = FillUpVerifier.Verify(fillUp, [segment], previousFillUp: null);

        Assert.False(result.IsVerified);
        Assert.Contains(result.Issues, i => i.Contains("time", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Verify_Fails_WhenThereAreNoSegments_ButAPreviousFillUpExists()
    {
        var t0 = DateTimeOffset.UtcNow;
        var previousFillUp = FillUpAt(DepotX, t0.AddHours(-2));
        var fillUp = FillUpAt(ClujNapoca, t0);

        // Zero segments would silently hide a real, undocumented trip — must be flagged.
        var result = FillUpVerifier.Verify(fillUp, [], previousFillUp);

        Assert.False(result.IsVerified);
        Assert.Contains(result.Issues, i => i.Contains("gap", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Verify_Fails_WhenThereIsAGapBetweenTwoSegments()
    {
        var t0 = DateTimeOffset.UtcNow;
        var fillUp = FillUpAt(ClujNapoca, t0);

        // Second segment starts nowhere near where the first one ended — a real gap.
        var segments = new List<RouteSegment>
        {
            SegmentBetween(DepotX, Brasov, t0.AddHours(-2), t0.AddHours(-1)),
            SegmentBetween(ClujNapoca, ClujNapoca, t0.AddMinutes(-30), t0)
        };

        var result = FillUpVerifier.Verify(fillUp, segments, previousFillUp: null);

        Assert.False(result.IsVerified);
        Assert.Contains(result.Issues, i => i.Contains("gap", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Verify_Fails_WhenTheFirstSegmentDoesNotStartAtThePreviousFillUpsStation()
    {
        var t0 = DateTimeOffset.UtcNow;
        var previousFillUp = FillUpAt(DepotX, t0.AddHours(-2));
        var fillUp = FillUpAt(ClujNapoca, t0);

        // Segment starts at Brasov, not at the previous fill-up's station (Depot X) — a gap.
        var segments = new List<RouteSegment> { SegmentBetween(Brasov, ClujNapoca, t0.AddHours(-1), t0) };

        var result = FillUpVerifier.Verify(fillUp, segments, previousFillUp);

        Assert.False(result.IsVerified);
        Assert.Contains(result.Issues, i => i.Contains("gap", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Verify_ReportsEveryIssue_NotJustTheFirstOne()
    {
        var t0 = DateTimeOffset.UtcNow;
        var fillUp = FillUpAt(ClujNapoca, t0);

        var badDistance = SegmentBetween(DepotX, Brasov, t0.AddHours(-2), t0.AddHours(-1));
        badDistance.DistanceKm = 0;
        var badTimestamps = SegmentBetween(Brasov, ClujNapoca, t0, t0.AddHours(-1));

        var result = FillUpVerifier.Verify(fillUp, [badDistance, badTimestamps], previousFillUp: null);

        Assert.False(result.IsVerified);
        Assert.True(result.Issues.Count >= 2);
    }
}

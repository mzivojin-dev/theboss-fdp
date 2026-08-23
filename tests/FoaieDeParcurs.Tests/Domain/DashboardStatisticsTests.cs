using FoaieDeParcurs.Core.Domain;
using FoaieDeParcurs.Core.Entities;

namespace FoaieDeParcurs.Tests.Domain;

public sealed class DashboardStatisticsTests
{
    private static FillUp FillUp(DateTimeOffset timestamp, decimal amountPaid) =>
        new() { Timestamp = timestamp, LitersFilled = 40, AmountPaid = amountPaid, CreatedAt = timestamp };

    private static RouteSegment Segment(DateTimeOffset endTimestamp, double distanceKm) =>
        new() { StartTimestamp = endTimestamp.AddHours(-1), EndTimestamp = endTimestamp, DistanceKm = distanceKm };

    [Fact]
    public void ForMonth_CountsOnlyFillUpsInThatCalendarMonth()
    {
        var fillUps = new List<FillUp>
        {
            FillUp(new DateTimeOffset(2026, 6, 5, 8, 0, 0, TimeSpan.Zero), 200m),
            FillUp(new DateTimeOffset(2026, 6, 20, 8, 0, 0, TimeSpan.Zero), 300m),
            FillUp(new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero), 400m)
        };

        var summary = DashboardStatistics.ForMonth(fillUps, segments: [], year: 2026, month: 6);

        Assert.Equal(2, summary.FillUpCount);
        Assert.Equal(500m, summary.TotalAmountPaid);
    }

    [Fact]
    public void ForMonth_SumsRouteSegmentDistance_ByTheSegmentsEndMonth()
    {
        var segments = new List<RouteSegment>
        {
            Segment(new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), 100),
            Segment(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero), 50),
            Segment(new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero), 999) // different month, excluded
        };

        var summary = DashboardStatistics.ForMonth([], segments, year: 2026, month: 6);

        Assert.Equal(150, summary.TotalDistanceKm);
    }

    [Fact]
    public void ForMonth_ReturnsZeroes_WhenNothingMatchesThatMonth()
    {
        var summary = DashboardStatistics.ForMonth([], [], year: 2026, month: 1);

        Assert.Equal(0, summary.FillUpCount);
        Assert.Equal(0, summary.TotalDistanceKm);
        Assert.Equal(0m, summary.TotalAmountPaid);
    }
}

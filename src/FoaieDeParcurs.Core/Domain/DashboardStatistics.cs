using FoaieDeParcurs.Core.Entities;

namespace FoaieDeParcurs.Core.Domain;

/// <summary>One calendar month's headline numbers for the Dashboard's stat cards.</summary>
public sealed record MonthlySummary(int Year, int Month, int FillUpCount, double TotalDistanceKm, decimal TotalAmountPaid);

/// <summary>
/// Pure aggregation over already-loaded fill-ups and route segments — no persistence, no
/// platform dependency, so it's testable with plain in-memory lists like the rest of Core.
/// </summary>
public static class DashboardStatistics
{
    /// <summary>
    /// Fill-ups are attributed to the month they happened in. Route segments are attributed to
    /// the month their leg *ended* in (matches how a segment is tied to the fill-up that
    /// triggered its capture) — a segment spanning a month boundary counts toward the month it
    /// finished in, the same granularity the printed Foaie de Parcurs already uses per period.
    /// </summary>
    public static MonthlySummary ForMonth(
        IReadOnlyList<FillUp> fillUps,
        IReadOnlyList<RouteSegment> segments,
        int year,
        int month)
    {
        var monthFillUps = fillUps.Where(f => f.Timestamp.Year == year && f.Timestamp.Month == month).ToList();
        var totalKm = segments
            .Where(s => s.EndTimestamp.Year == year && s.EndTimestamp.Month == month)
            .Sum(s => s.DistanceKm);
        var totalPaid = monthFillUps.Sum(f => f.AmountPaid);

        return new MonthlySummary(year, month, monthFillUps.Count, totalKm, totalPaid);
    }
}

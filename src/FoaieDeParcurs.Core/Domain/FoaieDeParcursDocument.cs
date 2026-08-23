namespace FoaieDeParcurs.Core.Domain;

/// <summary>One printed row in the Foaie de Parcurs route table.</summary>
public sealed record FoaieDeParcursSegmentRow(
    DateTimeOffset Date,
    string From,
    string To,
    double DistanceKm,
    string Purpose);

/// <summary>
/// Everything the PDF renderer needs to lay out one Foaie de Parcurs — assembled by
/// <see cref="FoaieDeParcursDocumentBuilder"/> from a <c>VehicleProfile</c>, a <c>FillUp</c>,
/// and its <c>RouteSegment</c>s. Deliberately not the PDF bytes themselves, so rendering can be
/// tested against a known document without QuestPDF and vice versa.
/// </summary>
public sealed record FoaieDeParcursDocument(
    string CompanyName,
    string Cui,
    string DriverName,
    string VehiclePlate,
    string VehicleMakeModel,
    string VehicleCategory,
    string FuelType,
    double FuelConsumptionNormPer100Km,
    DateTimeOffset IssueDate,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    IReadOnlyList<FoaieDeParcursSegmentRow> Segments,
    double LitersFilled,
    decimal AmountPaid,
    string Currency,
    string StationName,
    DateTimeOffset FillUpDate,
    double? OdometerReading)
{
    /// <summary>Total distance across every segment — the headline figure on the printed page.</summary>
    public double TotalDistanceKm => Segments.Sum(s => s.DistanceKm);
}

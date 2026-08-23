using FoaieDeParcurs.Core.Entities;

namespace FoaieDeParcurs.Core.Domain;

/// <summary>Assembles a <see cref="FoaieDeParcursDocument"/> from plain in-memory entities — no persistence, no PDF library.</summary>
public static class FoaieDeParcursDocumentBuilder
{
    public static FoaieDeParcursDocument Build(
        VehicleProfile profile,
        FillUp fillUp,
        FillUp? previousFillUp,
        IReadOnlyList<RouteSegment> segments)
    {
        var rows = segments
            .OrderBy(s => s.StartTimestamp)
            .Select(s => new FoaieDeParcursSegmentRow(s.StartTimestamp, s.StartLocationName, s.EndLocationName, s.DistanceKm, s.Purpose))
            .ToList();

        return new FoaieDeParcursDocument(
            CompanyName: profile.CompanyName,
            Cui: profile.Cui,
            DriverName: profile.DriverName,
            VehiclePlate: profile.VehiclePlate,
            VehicleMakeModel: profile.VehicleMakeModel,
            VehicleCategory: profile.VehicleCategory,
            FuelType: profile.FuelType.ToString(),
            FuelConsumptionNormPer100Km: profile.FuelConsumptionNormPer100Km,
            IssueDate: DateTimeOffset.Now,
            PeriodStart: previousFillUp?.Timestamp ?? fillUp.Timestamp,
            PeriodEnd: fillUp.Timestamp,
            Segments: rows,
            LitersFilled: fillUp.LitersFilled,
            AmountPaid: fillUp.AmountPaid,
            Currency: fillUp.Currency,
            StationName: fillUp.StationName ?? string.Empty,
            FillUpDate: fillUp.Timestamp,
            OdometerReading: fillUp.OdometerReading);
    }
}

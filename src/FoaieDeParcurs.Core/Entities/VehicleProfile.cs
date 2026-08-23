namespace FoaieDeParcurs.Core.Entities;

/// <summary>
/// The one-time company/vehicle/driver constants entered in Settings and reused on every
/// generated document and email. A single row (singleton) per install.
/// </summary>
public sealed class VehicleProfile
{
    public int Id { get; set; }

    public string CompanyName { get; set; } = string.Empty;
    public string Cui { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;

    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleMakeModel { get; set; } = string.Empty;
    public string VehicleCategory { get; set; } = string.Empty;

    public FuelType FuelType { get; set; } = FuelType.Benzina;
    public double FuelConsumptionNormPer100Km { get; set; }

    public string EmailRecipient { get; set; } = string.Empty;
    public string EmailSubjectTemplate { get; set; } = "Foaie de Parcurs - {PeriodStart} - {PeriodEnd}";
    public string EmailBodyTemplate { get; set; } =
        "Bună ziua,\n\nAtașat găsiți foaia de parcurs pentru perioada {PeriodStart} - {PeriodEnd}.\n\nCu stimă,\n{DriverName}";

    public ReportingCadence ReportingCadence { get; set; } = ReportingCadence.PerFillUp;
}

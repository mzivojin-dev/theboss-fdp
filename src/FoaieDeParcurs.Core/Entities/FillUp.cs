namespace FoaieDeParcurs.Core.Entities;

/// <summary>A single fuel purchase event, and the anchor point between which route segments are grouped.</summary>
public sealed class FillUp
{
    public int Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Set when the station was picked from a saved Known Location.</summary>
    public int? StationLocationId { get; set; }

    /// <summary>Free-text station name/address, used when no Known Location was picked (or to override its name).</summary>
    public string? StationName { get; set; }
    public double? StationLatitude { get; set; }
    public double? StationLongitude { get; set; }

    public double LitersFilled { get; set; }
    public decimal AmountPaid { get; set; }
    public string Currency { get; set; } = "RON";

    public string? ReceiptPhotoPath { get; set; }
    public double? OdometerReading { get; set; }
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>True only once every verification check in <c>FillUpVerifier</c> has passed.</summary>
    public bool IsVerified { get; set; }

    /// <summary>Best-effort: set once the driver confirms they sent the email. Android cannot confirm delivery.</summary>
    public bool EmailSent { get; set; }
}

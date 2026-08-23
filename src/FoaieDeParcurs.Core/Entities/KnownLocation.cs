namespace FoaieDeParcurs.Core.Entities;

/// <summary>
/// A user-defined named place (e.g. "Work — Depot X", "Home") that route segment
/// endpoints snap to when a GPS point falls within <see cref="RadiusMeters"/> of it.
/// </summary>
public sealed class KnownLocation
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>Matching radius, in meters. Defaults to 150m.</summary>
    public double RadiusMeters { get; set; } = 150;

    public KnownLocationType Type { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

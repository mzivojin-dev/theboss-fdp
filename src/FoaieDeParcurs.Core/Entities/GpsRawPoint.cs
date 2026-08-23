namespace FoaieDeParcurs.Core.Entities;

/// <summary>
/// A single raw GPS fix recorded while driving. Stored in a rolling buffer and purged
/// once it has been consumed into a <see cref="RouteSegment"/>'s simplified polyline.
/// </summary>
public sealed class GpsRawPoint
{
    public int Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Speed in meters/second, when the platform location API reports it.</summary>
    public double? Speed { get; set; }

    /// <summary>Horizontal accuracy in meters, as reported by the location provider.</summary>
    public double? Accuracy { get; set; }
}

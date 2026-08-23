namespace FoaieDeParcurs.Core.Domain;

/// <summary>Tuning knobs for <see cref="TripLedger"/>'s route-segment derivation.</summary>
public sealed class RouteSegmentationOptions
{
    public static RouteSegmentationOptions Default { get; } = new();

    /// <summary>How close consecutive points must stay to count as "parked" at one spot.</summary>
    public double StopRadiusMeters { get; init; } = 50;

    /// <summary>How long the vehicle must stay within <see cref="StopRadiusMeters"/> to count as a stop worth splitting a segment at.</summary>
    public TimeSpan StopDuration { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Segments shorter than this are dropped as GPS jitter rather than a real trip.</summary>
    public double MinimumSegmentDistanceMeters { get; init; } = 100;

    /// <summary>Douglas-Peucker tolerance used when simplifying each segment's polyline.</summary>
    public double DouglasPeuckerToleranceMeters { get; init; } = 15;
}

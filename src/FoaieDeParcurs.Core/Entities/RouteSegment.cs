namespace FoaieDeParcurs.Core.Entities;

/// <summary>
/// One leg of driving between two points, derived from the GPS trail between fill-ups
/// (or before the very first fill-up, in which case <see cref="StartFillUpId"/> is null).
/// </summary>
public sealed class RouteSegment
{
    public int Id { get; set; }

    /// <summary>Null for the trail recorded before the first fill-up ever exists.</summary>
    public int? StartFillUpId { get; set; }
    public int? EndFillUpId { get; set; }

    public string StartLocationName { get; set; } = string.Empty;
    public double StartLatitude { get; set; }
    public double StartLongitude { get; set; }
    public DateTimeOffset StartTimestamp { get; set; }

    public string EndLocationName { get; set; } = string.Empty;
    public double EndLatitude { get; set; }
    public double EndLongitude { get; set; }
    public DateTimeOffset EndTimestamp { get; set; }

    public double DistanceKm { get; set; }

    /// <summary>Simplified polyline (Douglas-Peucker'd) encoded as "lat,lng;lat,lng;...".</summary>
    public string? PolylineEncoded { get; set; }

    public string Purpose { get; set; } = "Deplasare de serviciu";
}

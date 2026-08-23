using FoaieDeParcurs.Core.Entities;

namespace FoaieDeParcurs.Core.Repositories;

/// <summary>
/// Persistence for the rolling raw-GPS-point buffer. Points are added continuously while
/// tracking and purged once consumed into a <see cref="RouteSegment"/>'s simplified polyline.
/// </summary>
public interface IGpsRawPointRepository
{
    Task AddAsync(GpsRawPoint point);

    /// <summary>All points at or after <paramref name="since"/>, ordered by timestamp — the trail since the last fill-up.</summary>
    Task<List<GpsRawPoint>> GetSinceAsync(DateTimeOffset since);

    /// <summary>Purges points at or before <paramref name="upTo"/> — call once they've been folded into a saved RouteSegment.</summary>
    Task PurgeUpToAsync(DateTimeOffset upTo);
}

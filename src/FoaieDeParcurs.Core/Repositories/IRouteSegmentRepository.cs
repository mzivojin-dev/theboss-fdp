using FoaieDeParcurs.Core.Entities;

namespace FoaieDeParcurs.Core.Repositories;

public interface IRouteSegmentRepository
{
    /// <summary>The segments ending at (i.e. belonging to) the given fill-up, in chronological order.</summary>
    Task<List<RouteSegment>> GetForFillUpAsync(int fillUpId);

    /// <summary>Every persisted segment — used for Dashboard statistics aggregation across all fill-ups.</summary>
    Task<List<RouteSegment>> GetAllAsync();
}

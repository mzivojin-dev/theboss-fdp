using FoaieDeParcurs.Core.Entities;

namespace FoaieDeParcurs.Core.Repositories;

public interface IFillUpRepository
{
    /// <summary>Newest first — backs the Home screen list.</summary>
    Task<List<FillUp>> GetAllAsync();

    Task<FillUp?> GetByIdAsync(int id);

    /// <summary>The most recently saved fill-up, or null if none exist yet.</summary>
    Task<FillUp?> GetMostRecentAsync();

    /// <summary>
    /// Saves the fill-up and its route segments as a single transaction: the fill-up is
    /// inserted first (to obtain its Id), each segment's <see cref="RouteSegment.EndFillUpId"/>
    /// is set to it, then the segments are inserted. Either both succeed or neither does.
    /// </summary>
    Task<FillUp> AddWithSegmentsAsync(FillUp fillUp, IReadOnlyList<RouteSegment> segments);

    /// <summary>
    /// Deletes the fill-up and the route segments that end at it (its trip). Segments that
    /// start at it (leading to the next fill-up) are kept but orphaned (StartFillUpId set to
    /// null) rather than deleted, since they still document real driving.
    /// </summary>
    Task DeleteAsync(int id);
}

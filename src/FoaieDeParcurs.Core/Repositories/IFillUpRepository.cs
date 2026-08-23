using FoaieDeParcurs.Core.Entities;

namespace FoaieDeParcurs.Core.Repositories;

public interface IFillUpRepository
{
    /// <summary>Newest first — backs the Home screen list.</summary>
    Task<List<FillUp>> GetAllAsync();

    Task<FillUp?> GetByIdAsync(int id);

    /// <summary>The most recently saved fill-up, or null if none exist yet.</summary>
    Task<FillUp?> GetMostRecentAsync();

    /// <summary>The fill-up immediately before the given timestamp, or null if it's the first one.</summary>
    Task<FillUp?> GetPreviousAsync(DateTimeOffset beforeTimestamp);

    /// <summary>
    /// Saves the fill-up and its route segments as a single transaction: the fill-up is
    /// inserted first (to obtain its Id), each segment's <see cref="RouteSegment.EndFillUpId"/>
    /// is set to it, then the segments are inserted. Either both succeed or neither does.
    /// </summary>
    Task<FillUp> AddWithSegmentsAsync(FillUp fillUp, IReadOnlyList<RouteSegment> segments);

    /// <summary>
    /// Replaces an already-saved fill-up's fields and its full set of route segments — used
    /// when the driver fixes a gap that verification flagged (see <c>FillUpVerifier</c>) and
    /// re-saves rather than creating a duplicate entry. Also one transaction.
    /// </summary>
    Task UpdateWithSegmentsAsync(FillUp fillUp, IReadOnlyList<RouteSegment> segments);

    /// <summary>Sets the fill-up's verified flag — the only place <see cref="FillUp.IsVerified"/> is written.</summary>
    Task SetVerifiedAsync(int id, bool isVerified);

    /// <summary>Best-effort: set once the driver confirms they sent the email (Android can't confirm delivery).</summary>
    Task SetEmailSentAsync(int id, bool emailSent);

    /// <summary>
    /// Deletes the fill-up and the route segments that end at it (its trip). Segments that
    /// start at it (leading to the next fill-up) are kept but orphaned (StartFillUpId set to
    /// null) rather than deleted, since they still document real driving.
    /// </summary>
    Task DeleteAsync(int id);
}

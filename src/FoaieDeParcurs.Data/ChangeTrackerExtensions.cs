using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FoaieDeParcurs.Data;

/// <summary>
/// EF Core's <c>DbSet&lt;T&gt;.Update()</c> throws "cannot be tracked because another instance
/// with the same key value ... is already being tracked" if a different instance with the same
/// key is already tracked by this context. That happens routinely here: <see cref="AppDbContext"/>
/// is registered as Scoped, but MAUI never creates a DI scope per page/operation, so the same
/// context instance lives for the whole app session. Any entity added or updated earlier (e.g.
/// <c>GetOrCreateAsync</c>'s initial insert) stays tracked, while every repository Save/Update
/// call builds a brand-new instance from scratch — the two collide. This was an unhandled crash
/// (see the Settings "Save" bug). Detach the stale tracked instance, if any, before attaching
/// the caller's fresh one.
/// </summary>
internal static class ChangeTrackerExtensions
{
    public static void DetachStaleTrackedInstance<TEntity>(
        this ChangeTracker changeTracker, TEntity replacement, Func<TEntity, int> idSelector)
        where TEntity : class
    {
        EntityEntry<TEntity>? stale = changeTracker.Entries<TEntity>()
            .FirstOrDefault(e => !ReferenceEquals(e.Entity, replacement) && idSelector(e.Entity) == idSelector(replacement));

        if (stale is not null)
        {
            stale.State = EntityState.Detached;
        }
    }
}

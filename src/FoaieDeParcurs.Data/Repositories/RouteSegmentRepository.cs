using FoaieDeParcurs.Core.Entities;
using FoaieDeParcurs.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FoaieDeParcurs.Data.Repositories;

public sealed class RouteSegmentRepository(AppDbContext db) : IRouteSegmentRepository
{
    public async Task<List<RouteSegment>> GetForFillUpAsync(int fillUpId) =>
        await db.RouteSegments
            .AsNoTracking()
            .Where(s => s.EndFillUpId == fillUpId)
            .OrderBy(s => s.StartTimestamp)
            .ToListAsync();
}

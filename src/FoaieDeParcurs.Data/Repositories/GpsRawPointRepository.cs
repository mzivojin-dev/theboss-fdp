using FoaieDeParcurs.Core.Entities;
using FoaieDeParcurs.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FoaieDeParcurs.Data.Repositories;

public sealed class GpsRawPointRepository(AppDbContext db) : IGpsRawPointRepository
{
    public async Task AddAsync(GpsRawPoint point)
    {
        db.GpsRawPoints.Add(point);
        await db.SaveChangesAsync();
    }

    public async Task<List<GpsRawPoint>> GetSinceAsync(DateTimeOffset since) =>
        await db.GpsRawPoints
            .AsNoTracking()
            .Where(p => p.Timestamp >= since)
            .OrderBy(p => p.Timestamp)
            .ToListAsync();

    public async Task PurgeUpToAsync(DateTimeOffset upTo)
    {
        await db.GpsRawPoints
            .Where(p => p.Timestamp <= upTo)
            .ExecuteDeleteAsync();
    }
}

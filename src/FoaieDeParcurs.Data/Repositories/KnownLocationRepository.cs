using FoaieDeParcurs.Core.Entities;
using FoaieDeParcurs.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FoaieDeParcurs.Data.Repositories;

public sealed class KnownLocationRepository(AppDbContext db) : IKnownLocationRepository
{
    public async Task<List<KnownLocation>> GetAllAsync() =>
        await db.KnownLocations.AsNoTracking().OrderBy(l => l.Name).ToListAsync();

    public async Task<KnownLocation?> GetByIdAsync(int id) =>
        await db.KnownLocations.AsNoTracking().SingleOrDefaultAsync(l => l.Id == id);

    public async Task<KnownLocation> AddAsync(KnownLocation location)
    {
        db.KnownLocations.Add(location);
        await db.SaveChangesAsync();
        return location;
    }

    public async Task UpdateAsync(KnownLocation location)
    {
        db.KnownLocations.Update(location);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var existing = await db.KnownLocations.FindAsync(id);
        if (existing is not null)
        {
            db.KnownLocations.Remove(existing);
            await db.SaveChangesAsync();
        }
    }
}

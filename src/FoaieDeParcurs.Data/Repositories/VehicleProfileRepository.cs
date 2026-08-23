using FoaieDeParcurs.Core.Entities;
using FoaieDeParcurs.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FoaieDeParcurs.Data.Repositories;

public sealed class VehicleProfileRepository(AppDbContext db) : IVehicleProfileRepository
{
    public async Task<VehicleProfile> GetOrCreateAsync()
    {
        var existing = await db.VehicleProfiles.AsNoTracking().FirstOrDefaultAsync();
        if (existing is not null)
        {
            return existing;
        }

        var created = new VehicleProfile();
        db.VehicleProfiles.Add(created);
        await db.SaveChangesAsync();
        return created;
    }

    public async Task SaveAsync(VehicleProfile profile)
    {
        db.VehicleProfiles.Update(profile);
        await db.SaveChangesAsync();
    }
}

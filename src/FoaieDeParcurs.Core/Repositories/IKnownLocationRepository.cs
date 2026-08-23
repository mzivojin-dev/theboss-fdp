using FoaieDeParcurs.Core.Entities;

namespace FoaieDeParcurs.Core.Repositories;

/// <summary>
/// Persistence contract for Known Locations. Defined here (not in Data) so view models can
/// depend on the abstraction — the interface itself has no persistence-framework dependency.
/// </summary>
public interface IKnownLocationRepository
{
    Task<List<KnownLocation>> GetAllAsync();
    Task<KnownLocation?> GetByIdAsync(int id);
    Task<KnownLocation> AddAsync(KnownLocation location);
    Task UpdateAsync(KnownLocation location);
    Task DeleteAsync(int id);
}

using FoaieDeParcurs.Core.Entities;

namespace FoaieDeParcurs.Core.Repositories;

/// <summary>
/// Persistence contract for the single <see cref="VehicleProfile"/> row this app keeps.
/// </summary>
public interface IVehicleProfileRepository
{
    /// <summary>Returns the existing profile, or creates and persists a default one if none exists yet.</summary>
    Task<VehicleProfile> GetOrCreateAsync();

    Task SaveAsync(VehicleProfile profile);
}

namespace FoaieDeParcurs.Core.Domain;

/// <summary>
/// Resolves a human-readable name for a coordinate that didn't match any Known Location.
/// Implemented by the offline <see cref="RomanianCityGazetteer"/> (pure, no I/O — used by
/// TripLedger's synthetic-trace tests and as the always-available fallback) and, at the app
/// layer, by a live reverse-geocoding adapter tried first when network/API key are available.
/// </summary>
public interface ILocationNamer
{
    string ResolveName(double latitude, double longitude);
}

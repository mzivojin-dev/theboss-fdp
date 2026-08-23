namespace FoaieDeParcurs.Core.Domain;

/// <summary>
/// Offline nearest-neighbor lookup against a bundled list of Romanian cities/towns. Used when
/// a route segment endpoint isn't near any Known Location and no live geocoding is available
/// (no Maps API key, or no network) — see spec's "offline city-lookup fallback".
/// </summary>
public sealed class RomanianCityGazetteer : ILocationNamer
{
    private static readonly (string Name, double Latitude, double Longitude)[] Cities =
    [
        ("Bucuresti", 44.4268, 26.1025),
        ("Cluj-Napoca", 46.7712, 23.6236),
        ("Timisoara", 45.7489, 21.2087),
        ("Iasi", 47.1585, 27.6014),
        ("Constanta", 44.1765, 28.6348),
        ("Craiova", 44.3302, 23.7949),
        ("Brasov", 45.6427, 25.5887),
        ("Galati", 45.4353, 28.0080),
        ("Ploiesti", 44.9414, 26.0225),
        ("Oradea", 47.0465, 21.9189),
        ("Braila", 45.2692, 27.9575),
        ("Arad", 46.1866, 21.3123),
        ("Pitesti", 44.8565, 24.8692),
        ("Sibiu", 45.7983, 24.1256),
        ("Bacau", 46.5670, 26.9146),
        ("Targu Mures", 46.5527, 24.5575),
        ("Baia Mare", 47.6567, 23.5847),
        ("Buzau", 45.1500, 26.8167),
        ("Botosani", 47.7486, 26.6693),
        ("Satu Mare", 47.7920, 22.8850),
        ("Ramnicu Valcea", 45.1046, 24.3751),
        ("Drobeta-Turnu Severin", 44.6369, 22.6597),
        ("Suceava", 47.6514, 26.2556),
        ("Piatra Neamt", 46.9276, 26.3707),
        ("Targu Jiu", 45.0417, 23.2733),
        ("Focsani", 45.6967, 27.1858),
        ("Bistrita", 47.1333, 24.5000),
        ("Resita", 45.3000, 21.8833),
        ("Slatina", 44.4333, 24.3667),
        ("Calarasi", 44.2058, 27.3306),
        ("Giurgiu", 43.9037, 25.9699),
        ("Deva", 45.8833, 22.9000),
        ("Hunedoara", 45.7500, 22.9000),
        ("Zalau", 47.1911, 23.0572),
        ("Sfantu Gheorghe", 45.8667, 25.7833),
        ("Alba Iulia", 46.0667, 23.5833),
        ("Vaslui", 46.6407, 27.7276),
        ("Tulcea", 45.1667, 28.8000),
        ("Slobozia", 44.5639, 27.3667),
        ("Alexandria", 43.9642, 25.3336),
        ("Miercurea Ciuc", 46.3597, 25.8022),
    ];

    public string ResolveName(double latitude, double longitude)
    {
        var nearest = Cities[0];
        var nearestDistance = GeoMath.HaversineDistanceMeters(latitude, longitude, nearest.Latitude, nearest.Longitude);

        foreach (var city in Cities.AsSpan(1))
        {
            var distance = GeoMath.HaversineDistanceMeters(latitude, longitude, city.Latitude, city.Longitude);
            if (distance < nearestDistance)
            {
                nearest = city;
                nearestDistance = distance;
            }
        }

        return nearest.Name;
    }
}

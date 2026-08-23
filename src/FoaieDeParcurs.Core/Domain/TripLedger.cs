using System.Globalization;
using FoaieDeParcurs.Core.Entities;

namespace FoaieDeParcurs.Core.Domain;

/// <summary>
/// The pure domain engine: turns a raw GPS trail into named, distance-computed route segments.
/// Takes only in-memory data — no SQLite, no Android APIs, no PDF library — so it can be
/// exercised with synthetic GPS traces (see FoaieDeParcurs.Tests.Domain.TripLedgerTests).
/// </summary>
public static class TripLedger
{
    /// <summary>
    /// Splits the GPS trail since the previous fill-up into one or more route segments.
    /// A segment boundary is inserted wherever the vehicle stays within
    /// <see cref="RouteSegmentationOptions.StopRadiusMeters"/> of one spot for at least
    /// <see cref="RouteSegmentationOptions.StopDuration"/> — a genuine stop, not GPS jitter or
    /// a momentary signal gap. Each endpoint is named from the nearest Known Location within
    /// its radius, or via <paramref name="namer"/> otherwise (see <see cref="ILocationNamer"/>).
    /// </summary>
    /// <param name="startFillUpId">The fill-up this trail starts after, or null for the very first trail before any fill-up exists.</param>
    /// <param name="endFillUpId">The fill-up this trail is being derived for.</param>
    public static List<RouteSegment> DeriveRouteSegments(
        IReadOnlyList<GpsRawPoint> points,
        IReadOnlyList<KnownLocation> knownLocations,
        ILocationNamer namer,
        int? startFillUpId,
        int? endFillUpId,
        RouteSegmentationOptions? options = null)
    {
        options ??= RouteSegmentationOptions.Default;

        if (points.Count < 2)
        {
            return [];
        }

        var ordered = points.OrderBy(p => p.Timestamp).ToList();
        var stops = FindStops(ordered, options);

        var boundaries = new List<int> { 0 };
        foreach (var (stopStart, stopEnd) in stops)
        {
            boundaries.Add(stopStart);
            boundaries.Add(stopEnd);
        }
        boundaries.Add(ordered.Count - 1);

        var segments = new List<RouteSegment>();

        for (var i = 0; i < boundaries.Count - 1; i++)
        {
            var startIndex = boundaries[i];
            var endIndex = boundaries[i + 1];
            if (endIndex <= startIndex)
            {
                continue;
            }

            var raw = ordered.GetRange(startIndex, endIndex - startIndex + 1);
            var simplified = GeoMath.Simplify(
                raw.Select(p => (p.Latitude, p.Longitude, p.Timestamp)).ToList(),
                options.DouglasPeuckerToleranceMeters);

            var distanceMeters = 0.0;
            for (var k = 1; k < simplified.Count; k++)
            {
                distanceMeters += GeoMath.HaversineDistanceMeters(
                    simplified[k - 1].Latitude, simplified[k - 1].Longitude,
                    simplified[k].Latitude, simplified[k].Longitude);
            }

            if (distanceMeters < options.MinimumSegmentDistanceMeters)
            {
                continue;
            }

            var start = ordered[startIndex];
            var end = ordered[endIndex];

            segments.Add(new RouteSegment
            {
                StartFillUpId = startFillUpId,
                EndFillUpId = endFillUpId,
                StartLocationName = ResolveEndpointName(start, knownLocations, namer),
                StartLatitude = start.Latitude,
                StartLongitude = start.Longitude,
                StartTimestamp = start.Timestamp,
                EndLocationName = ResolveEndpointName(end, knownLocations, namer),
                EndLatitude = end.Latitude,
                EndLongitude = end.Longitude,
                EndTimestamp = end.Timestamp,
                DistanceKm = distanceMeters / 1000.0,
                PolylineEncoded = EncodePolyline(simplified)
            });
        }

        return segments;
    }

    /// <summary>
    /// Finds maximal runs of points that stay within <see cref="RouteSegmentationOptions.StopRadiusMeters"/>
    /// of their first point for at least <see cref="RouteSegmentationOptions.StopDuration"/>.
    /// A large time gap between two spatially-distant points (a tunnel, a dead zone) is not a
    /// stop — only spatial clustering counts.
    /// </summary>
    private static List<(int Start, int End)> FindStops(IReadOnlyList<GpsRawPoint> ordered, RouteSegmentationOptions options)
    {
        var stops = new List<(int, int)>();
        var i = 0;

        while (i < ordered.Count)
        {
            var j = i;
            while (j + 1 < ordered.Count &&
                   GeoMath.HaversineDistanceMeters(
                       ordered[i].Latitude, ordered[i].Longitude,
                       ordered[j + 1].Latitude, ordered[j + 1].Longitude) <= options.StopRadiusMeters)
            {
                j++;
            }

            if (ordered[j].Timestamp - ordered[i].Timestamp >= options.StopDuration)
            {
                stops.Add((i, j));
                i = j + 1;
            }
            else
            {
                i++;
            }
        }

        return stops;
    }

    private static string ResolveEndpointName(GpsRawPoint point, IReadOnlyList<KnownLocation> knownLocations, ILocationNamer namer)
    {
        KnownLocation? nearest = null;
        var nearestDistance = double.MaxValue;

        foreach (var location in knownLocations)
        {
            var distance = GeoMath.HaversineDistanceMeters(point.Latitude, point.Longitude, location.Latitude, location.Longitude);
            if (distance <= location.RadiusMeters && distance < nearestDistance)
            {
                nearest = location;
                nearestDistance = distance;
            }
        }

        return nearest?.Name ?? namer.ResolveName(point.Latitude, point.Longitude);
    }

    private static string EncodePolyline(IEnumerable<(double Latitude, double Longitude, DateTimeOffset Timestamp)> points) =>
        string.Join(';', points.Select(p =>
            string.Create(CultureInfo.InvariantCulture, $"{p.Latitude},{p.Longitude}")));
}

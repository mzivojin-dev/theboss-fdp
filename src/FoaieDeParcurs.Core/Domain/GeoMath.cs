namespace FoaieDeParcurs.Core.Domain;

/// <summary>Pure geometry helpers shared by <see cref="TripLedger"/>. No I/O, no platform dependency.</summary>
public static class GeoMath
{
    private const double EarthRadiusMeters = 6_371_000;

    /// <summary>Great-circle distance between two points, in meters.</summary>
    public static double HaversineDistanceMeters(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLng = DegreesToRadians(lng2 - lng1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusMeters * c;
    }

    /// <summary>
    /// Douglas-Peucker polyline simplification. Distances use the haversine metric, which is
    /// accurate enough at the scale (tens of meters to tens of kilometers) this app operates at.
    /// Always keeps the first and last point.
    /// </summary>
    public static List<(double Latitude, double Longitude, DateTimeOffset Timestamp)> Simplify(
        IReadOnlyList<(double Latitude, double Longitude, DateTimeOffset Timestamp)> points,
        double toleranceMeters)
    {
        if (points.Count < 3)
        {
            return points.ToList();
        }

        var keep = new bool[points.Count];
        keep[0] = true;
        keep[^1] = true;
        SimplifyRange(points, 0, points.Count - 1, toleranceMeters, keep);

        var result = new List<(double, double, DateTimeOffset)>(points.Count);
        for (var i = 0; i < points.Count; i++)
        {
            if (keep[i])
            {
                result.Add(points[i]);
            }
        }

        return result;
    }

    private static void SimplifyRange(
        IReadOnlyList<(double Latitude, double Longitude, DateTimeOffset Timestamp)> points,
        int startIndex,
        int endIndex,
        double toleranceMeters,
        bool[] keep)
    {
        if (endIndex <= startIndex + 1)
        {
            return;
        }

        var start = points[startIndex];
        var end = points[endIndex];

        var maxDistance = 0.0;
        var maxIndex = -1;

        for (var i = startIndex + 1; i < endIndex; i++)
        {
            var distance = PerpendicularDistanceMeters(points[i], start, end);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                maxIndex = i;
            }
        }

        if (maxIndex == -1 || maxDistance <= toleranceMeters)
        {
            return;
        }

        keep[maxIndex] = true;
        SimplifyRange(points, startIndex, maxIndex, toleranceMeters, keep);
        SimplifyRange(points, maxIndex, endIndex, toleranceMeters, keep);
    }

    /// <summary>
    /// Approximates perpendicular distance from a point to the start-end line using an
    /// equirectangular projection local to the segment — accurate enough for the short spans
    /// Douglas-Peucker compares here, and far cheaper than great-circle cross-track math.
    /// </summary>
    private static double PerpendicularDistanceMeters(
        (double Latitude, double Longitude, DateTimeOffset Timestamp) point,
        (double Latitude, double Longitude, DateTimeOffset Timestamp) lineStart,
        (double Latitude, double Longitude, DateTimeOffset Timestamp) lineEnd)
    {
        var refLat = DegreesToRadians(lineStart.Latitude);
        var metersPerDegreeLat = EarthRadiusMeters * Math.PI / 180.0;
        var metersPerDegreeLng = metersPerDegreeLat * Math.Cos(refLat);

        var x0 = (point.Longitude - lineStart.Longitude) * metersPerDegreeLng;
        var y0 = (point.Latitude - lineStart.Latitude) * metersPerDegreeLat;
        var x1 = 0.0;
        var y1 = 0.0;
        var x2 = (lineEnd.Longitude - lineStart.Longitude) * metersPerDegreeLng;
        var y2 = (lineEnd.Latitude - lineStart.Latitude) * metersPerDegreeLat;

        var dx = x2 - x1;
        var dy = y2 - y1;

        if (dx == 0 && dy == 0)
        {
            return Math.Sqrt((x0 - x1) * (x0 - x1) + (y0 - y1) * (y0 - y1));
        }

        var t = ((x0 - x1) * dx + (y0 - y1) * dy) / (dx * dx + dy * dy);
        var closestX = x1 + t * dx;
        var closestY = y1 + t * dy;

        return Math.Sqrt((x0 - closestX) * (x0 - closestX) + (y0 - closestY) * (y0 - closestY));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}

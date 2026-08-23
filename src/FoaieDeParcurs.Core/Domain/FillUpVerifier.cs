using FoaieDeParcurs.Core.Entities;

namespace FoaieDeParcurs.Core.Domain;

/// <summary>
/// Runs the explicit verification checks a fill-up must pass before "Generate PDF &amp; Email"
/// is enabled (spec's verification requirement — never silently trust that a save succeeded).
/// Pure: takes the fill-up and its segments as plain data, so the DB re-read that proves they
/// were actually persisted happens at the call site (see FillUpRepository), not in here.
/// </summary>
public static class FillUpVerifier
{
    public static VerificationResult Verify(
        FillUp fillUp,
        IReadOnlyList<RouteSegment> segments,
        FillUp? previousFillUp,
        VerificationOptions? options = null)
    {
        options ??= VerificationOptions.Default;
        var issues = new List<string>();

        var ordered = segments.OrderBy(s => s.StartTimestamp).ToList();

        foreach (var segment in ordered)
        {
            issues.AddRange(CheckWellFormed(segment));
        }

        issues.AddRange(CheckContinuity(fillUp, ordered, previousFillUp, options));

        return issues.Count == 0 ? VerificationResult.Passed() : VerificationResult.Failed(issues);
    }

    private static IEnumerable<string> CheckWellFormed(RouteSegment segment)
    {
        var label = $"{segment.StartLocationName} → {segment.EndLocationName}";

        if (segment.DistanceKm <= 0)
        {
            yield return $"Segmentul \"{label}\" nu are nicio distanță înregistrată.";
        }

        if (segment.StartTimestamp == default || segment.EndTimestamp == default)
        {
            yield return $"Segmentul \"{label}\" nu are oră de plecare sau sosire.";
        }
        else if (segment.EndTimestamp <= segment.StartTimestamp)
        {
            yield return $"Segmentul \"{label}\" se termină înainte să înceapă — verificați ora.";
        }

        if (string.IsNullOrWhiteSpace(segment.StartLocationName) || string.IsNullOrWhiteSpace(segment.EndLocationName))
        {
            yield return "Unui segment îi lipsește locația de plecare sau de sosire.";
        }
    }

    private static IEnumerable<string> CheckContinuity(
        FillUp fillUp, List<RouteSegment> ordered, FillUp? previousFillUp, VerificationOptions options)
    {
        if (previousFillUp is { StationLatitude: double prevLat, StationLongitude: double prevLng })
        {
            if (ordered.Count == 0)
            {
                // A previous fill-up exists, so *some* driving happened to reach this one —
                // zero segments would silently hide that trip rather than document it.
                yield return "Decalaj: nu a fost înregistrat niciun segment de traseu de la alimentarea anterioară — adăugați segmentul lipsă pentru această deplasare.";
            }
            else
            {
                var first = ordered[0];
                var distance = GeoMath.HaversineDistanceMeters(prevLat, prevLng, first.StartLatitude, first.StartLongitude);
                if (distance > options.ContinuityToleranceMeters)
                {
                    yield return
                        $"Decalaj: stația alimentării anterioare nu corespunde cu începutul \"{first.StartLocationName}\" — " +
                        "adăugați segmentul lipsă pentru această deplasare.";
                }
            }
        }

        for (var i = 1; i < ordered.Count; i++)
        {
            var previous = ordered[i - 1];
            var current = ordered[i];
            var distance = GeoMath.HaversineDistanceMeters(
                previous.EndLatitude, previous.EndLongitude, current.StartLatitude, current.StartLongitude);

            if (distance > options.ContinuityToleranceMeters)
            {
                yield return
                    $"Decalaj între \"{previous.EndLocationName}\" și \"{current.StartLocationName}\" — " +
                    "adăugați segmentul lipsă pentru această deplasare.";
            }
        }
    }
}

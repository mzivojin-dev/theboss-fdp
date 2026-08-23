using FoaieDeParcurs.Core.Domain;
using FoaieDeParcurs.Core.Entities;
using FoaieDeParcurs.Data;
using FoaieDeParcurs.Data.Repositories;
using Microsoft.Data.Sqlite;

namespace FoaieDeParcurs.Tests.Domain;

/// <summary>
/// The exact scenario the spec's verification requirement calls for: insert a fill-up,
/// immediately re-read it back from a real SQLite DB (not trust that the insert didn't throw),
/// assert its linked route segments are attached, and assert the verification check passes —
/// plus the negative case, a deliberately broken chain that must fail with a named gap.
/// </summary>
public sealed class FillUpVerificationIntegrationTests : IDisposable
{
    private static readonly (double Lat, double Lng) DepotX = (44.4268, 26.1025);
    private static readonly (double Lat, double Lng) ClujNapoca = (46.7712, 23.6236);

    private readonly string _dbPath;

    public FillUpVerificationIntegrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"foaiedeparcurs-test-{Guid.NewGuid():N}.db3");
        using var db = AppDbContextFactory.Create(_dbPath);
        db.InitializeAsync().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public async Task InsertReReadAndVerify_PassesForAWellFormedContinuousFillUp()
    {
        var t0 = DateTimeOffset.UtcNow;
        var fillUpRepository = new FillUpRepository(AppDbContextFactory.Create(_dbPath));

        var fillUp = new FillUp
        {
            Timestamp = t0,
            StationLatitude = ClujNapoca.Lat,
            StationLongitude = ClujNapoca.Lng,
            LitersFilled = 42.5,
            AmountPaid = 320.75m,
            CreatedAt = t0
        };
        var segment = new RouteSegment
        {
            StartLocationName = "Depot X",
            StartLatitude = DepotX.Lat,
            StartLongitude = DepotX.Lng,
            StartTimestamp = t0.AddHours(-1),
            EndLocationName = "Cluj-Napoca",
            EndLatitude = ClujNapoca.Lat,
            EndLongitude = ClujNapoca.Lng,
            EndTimestamp = t0,
            DistanceKm = 330
        };

        var saved = await fillUpRepository.AddWithSegmentsAsync(fillUp, [segment]);

        // Re-read from a fresh repository/context — don't just trust the insert didn't throw.
        var readRepository = new FillUpRepository(AppDbContextFactory.Create(_dbPath));
        var reloadedFillUp = await readRepository.GetByIdAsync(saved.Id);
        Assert.NotNull(reloadedFillUp);

        var segmentRepository = new RouteSegmentRepository(AppDbContextFactory.Create(_dbPath));
        var reloadedSegments = await segmentRepository.GetForFillUpAsync(saved.Id);
        Assert.Single(reloadedSegments);

        var result = FillUpVerifier.Verify(reloadedFillUp!, reloadedSegments, previousFillUp: null);

        Assert.True(result.IsVerified);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task InsertReReadAndVerify_FailsAndNamesTheGap_ForADeliberatelyBrokenChain()
    {
        var t0 = DateTimeOffset.UtcNow;
        var fillUpRepository = new FillUpRepository(AppDbContextFactory.Create(_dbPath));

        var previousFillUp = await fillUpRepository.AddWithSegmentsAsync(
            new FillUp { Timestamp = t0.AddHours(-3), StationLatitude = DepotX.Lat, StationLongitude = DepotX.Lng, LitersFilled = 30, AmountPaid = 200, CreatedAt = t0 },
            []);

        var fillUp = new FillUp
        {
            Timestamp = t0,
            StationLatitude = ClujNapoca.Lat,
            StationLongitude = ClujNapoca.Lng,
            LitersFilled = 42.5,
            AmountPaid = 320.75m,
            CreatedAt = t0
        };

        // Deliberately broken: this segment doesn't start anywhere near the previous fill-up's
        // station (Depot X) — a real gap in the documented route.
        var brokenSegment = new RouteSegment
        {
            StartLocationName = "Somewhere else entirely",
            StartLatitude = 0,
            StartLongitude = 0,
            StartTimestamp = t0.AddHours(-1),
            EndLocationName = "Cluj-Napoca",
            EndLatitude = ClujNapoca.Lat,
            EndLongitude = ClujNapoca.Lng,
            EndTimestamp = t0,
            DistanceKm = 5000
        };

        var saved = await fillUpRepository.AddWithSegmentsAsync(fillUp, [brokenSegment]);

        var readRepository = new FillUpRepository(AppDbContextFactory.Create(_dbPath));
        var reloadedFillUp = await readRepository.GetByIdAsync(saved.Id);
        var segmentRepository = new RouteSegmentRepository(AppDbContextFactory.Create(_dbPath));
        var reloadedSegments = await segmentRepository.GetForFillUpAsync(saved.Id);

        var result = FillUpVerifier.Verify(reloadedFillUp!, reloadedSegments, previousFillUp);

        Assert.False(result.IsVerified);
        Assert.Contains(result.Issues, i => i.Contains("gap", StringComparison.OrdinalIgnoreCase));
    }
}

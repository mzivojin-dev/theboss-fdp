using FoaieDeParcurs.Core.Entities;
using FoaieDeParcurs.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FoaieDeParcurs.Tests.Data;

/// <summary>
/// Confirms the schema exists and every entity can be written and independently read back
/// from a real (temp-file) SQLite database — the same round-trip pattern the verification
/// flow (ticket #7) depends on.
/// </summary>
public sealed class AppDatabaseTests : IDisposable
{
    private readonly string _dbPath;

    public AppDatabaseTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"foaiedeparcurs-test-{Guid.NewGuid():N}.db3");
    }

    public void Dispose()
    {
        // Microsoft.Data.Sqlite pools connections by file path, keeping a handle open past
        // the DbContext's Dispose — clear the pool first so the temp file can be deleted.
        SqliteConnection.ClearAllPools();

        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public async Task InitializeAsync_CreatesAllTablesAndAllowsRoundTrip()
    {
        await using (var db = AppDbContextFactory.Create(_dbPath))
        {
            await db.InitializeAsync();
        }

        int locationId, fillUpId, segmentId, rawPointId, profileId;

        await using (var db = AppDbContextFactory.Create(_dbPath))
        {
            var location = new KnownLocation
            {
                Name = "Depot X",
                Latitude = 44.4268,
                Longitude = 26.1025,
                RadiusMeters = 150,
                Type = KnownLocationType.Work,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.KnownLocations.Add(location);

            var fillUp = new FillUp
            {
                Timestamp = DateTimeOffset.UtcNow,
                LitersFilled = 42.5,
                AmountPaid = 320.75m,
                Currency = "RON",
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.FillUps.Add(fillUp);

            var rawPoint = new GpsRawPoint
            {
                Latitude = 44.4268,
                Longitude = 26.1025,
                Timestamp = DateTimeOffset.UtcNow,
                Speed = 12.3,
                Accuracy = 5.0
            };
            db.GpsRawPoints.Add(rawPoint);

            var profile = new VehicleProfile
            {
                CompanyName = "Acme SRL",
                Cui = "RO12345678",
                DriverName = "Mihai Zivojinovic",
                VehiclePlate = "B-01-ABC",
                FuelConsumptionNormPer100Km = 7.5
            };
            db.VehicleProfiles.Add(profile);

            await db.SaveChangesAsync();

            var segment = new RouteSegment
            {
                EndFillUpId = fillUp.Id,
                StartLocationName = "Depot X",
                StartLatitude = 44.4268,
                StartLongitude = 26.1025,
                StartTimestamp = DateTimeOffset.UtcNow.AddHours(-1),
                EndLocationName = "Cluj-Napoca",
                EndLatitude = 46.7712,
                EndLongitude = 23.6236,
                EndTimestamp = DateTimeOffset.UtcNow
            };
            db.RouteSegments.Add(segment);
            await db.SaveChangesAsync();

            locationId = location.Id;
            fillUpId = fillUp.Id;
            segmentId = segment.Id;
            rawPointId = rawPoint.Id;
            profileId = profile.Id;
        }

        Assert.True(locationId > 0);
        Assert.True(fillUpId > 0);
        Assert.True(segmentId > 0);
        Assert.True(rawPointId > 0);
        Assert.True(profileId > 0);

        // Re-open a fresh context (new connection, no first-level cache) to prove this is a
        // real round trip through SQLite, not just reading back an in-memory object graph.
        await using var readBackDb = AppDbContextFactory.Create(_dbPath);

        var reloadedLocation = await readBackDb.KnownLocations.SingleAsync(x => x.Id == locationId);
        Assert.Equal("Depot X", reloadedLocation.Name);
        Assert.Equal(KnownLocationType.Work, reloadedLocation.Type);

        var reloadedFillUp = await readBackDb.FillUps.SingleAsync(x => x.Id == fillUpId);
        Assert.Equal(42.5, reloadedFillUp.LitersFilled);
        Assert.Equal(320.75m, reloadedFillUp.AmountPaid);
        Assert.False(reloadedFillUp.IsVerified);

        var reloadedSegment = await readBackDb.RouteSegments.SingleAsync(x => x.Id == segmentId);
        Assert.Equal("Deplasare de serviciu", reloadedSegment.Purpose);
        Assert.Equal(fillUpId, reloadedSegment.EndFillUpId);

        var reloadedRawPoint = await readBackDb.GpsRawPoints.SingleAsync(x => x.Id == rawPointId);
        Assert.Equal(12.3, reloadedRawPoint.Speed);

        var reloadedProfile = await readBackDb.VehicleProfiles.SingleAsync(x => x.Id == profileId);
        Assert.Equal("Acme SRL", reloadedProfile.CompanyName);
        Assert.Equal(7.5, reloadedProfile.FuelConsumptionNormPer100Km);
    }
}

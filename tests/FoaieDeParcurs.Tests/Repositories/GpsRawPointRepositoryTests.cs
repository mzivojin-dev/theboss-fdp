using FoaieDeParcurs.Core.Entities;
using FoaieDeParcurs.Data;
using FoaieDeParcurs.Data.Repositories;
using Microsoft.Data.Sqlite;

namespace FoaieDeParcurs.Tests.Repositories;

public sealed class GpsRawPointRepositoryTests : IDisposable
{
    private readonly string _dbPath;

    public GpsRawPointRepositoryTests()
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

    private GpsRawPointRepository CreateRepository() => new(AppDbContextFactory.Create(_dbPath));

    [Fact]
    public async Task GetSinceAsync_ReturnsOnlyPointsAtOrAfterTheGivenTimestamp_InTimestampOrder()
    {
        var baseTime = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);
        var addRepository = CreateRepository();

        await addRepository.AddAsync(new GpsRawPoint { Latitude = 1, Longitude = 1, Timestamp = baseTime.AddMinutes(-10) }); // before cutoff
        await addRepository.AddAsync(new GpsRawPoint { Latitude = 3, Longitude = 3, Timestamp = baseTime.AddMinutes(10) });
        await addRepository.AddAsync(new GpsRawPoint { Latitude = 2, Longitude = 2, Timestamp = baseTime }); // exactly at cutoff

        var readRepository = CreateRepository();
        var since = await readRepository.GetSinceAsync(baseTime);

        Assert.Equal(2, since.Count);
        Assert.Equal(2, since[0].Latitude);
        Assert.Equal(3, since[1].Latitude);
    }

    [Fact]
    public async Task PurgeUpToAsync_RemovesOnlyPointsAtOrBeforeTheGivenTimestamp()
    {
        var baseTime = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);
        var addRepository = CreateRepository();

        await addRepository.AddAsync(new GpsRawPoint { Latitude = 1, Longitude = 1, Timestamp = baseTime });
        await addRepository.AddAsync(new GpsRawPoint { Latitude = 2, Longitude = 2, Timestamp = baseTime.AddMinutes(5) });

        var purgeRepository = CreateRepository();
        await purgeRepository.PurgeUpToAsync(baseTime);

        var readRepository = CreateRepository();
        var remaining = await readRepository.GetSinceAsync(DateTimeOffset.MinValue);

        var remainingPoint = Assert.Single(remaining);
        Assert.Equal(2, remainingPoint.Latitude);
    }
}

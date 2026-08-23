using FoaieDeParcurs.Core.Entities;
using FoaieDeParcurs.Data;
using FoaieDeParcurs.Data.Repositories;
using Microsoft.Data.Sqlite;

namespace FoaieDeParcurs.Tests.Repositories;

public sealed class RouteSegmentRepositoryTests : IDisposable
{
    private readonly string _dbPath;

    public RouteSegmentRepositoryTests()
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
    public async Task GetForFillUpAsync_ReturnsOnlySegmentsEndingAtThatFillUp_InChronologicalOrder()
    {
        var fillUpRepository = new FillUpRepository(AppDbContextFactory.Create(_dbPath));
        var target = await fillUpRepository.AddWithSegmentsAsync(
            new FillUp { Timestamp = DateTimeOffset.UtcNow, LitersFilled = 40, AmountPaid = 300, CreatedAt = DateTimeOffset.UtcNow },
            [
                new RouteSegment
                {
                    StartLocationName = "Depot X", StartLatitude = 1, StartLongitude = 1, StartTimestamp = DateTimeOffset.UtcNow.AddHours(-3),
                    EndLocationName = "Brasov", EndLatitude = 2, EndLongitude = 2, EndTimestamp = DateTimeOffset.UtcNow.AddHours(-2), DistanceKm = 10
                },
                new RouteSegment
                {
                    StartLocationName = "Brasov", StartLatitude = 2, StartLongitude = 2, StartTimestamp = DateTimeOffset.UtcNow.AddHours(-1),
                    EndLocationName = "Cluj-Napoca", EndLatitude = 3, EndLongitude = 3, EndTimestamp = DateTimeOffset.UtcNow, DistanceKm = 20
                }
            ]);

        var other = await fillUpRepository.AddWithSegmentsAsync(
            new FillUp { Timestamp = DateTimeOffset.UtcNow.AddDays(1), LitersFilled = 30, AmountPaid = 200, CreatedAt = DateTimeOffset.UtcNow },
            [
                new RouteSegment
                {
                    StartLocationName = "Cluj-Napoca", StartLatitude = 3, StartLongitude = 3, StartTimestamp = DateTimeOffset.UtcNow,
                    EndLocationName = "Depot X", EndLatitude = 1, EndLongitude = 1, EndTimestamp = DateTimeOffset.UtcNow.AddHours(1), DistanceKm = 30
                }
            ]);

        var repository = new RouteSegmentRepository(AppDbContextFactory.Create(_dbPath));
        var segments = await repository.GetForFillUpAsync(target.Id);

        Assert.Equal(2, segments.Count);
        Assert.Equal("Depot X", segments[0].StartLocationName);
        Assert.Equal("Brasov", segments[1].StartLocationName);
        Assert.DoesNotContain(segments, s => s.EndFillUpId == other.Id);
    }
}

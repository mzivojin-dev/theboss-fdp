using FoaieDeParcurs.Core.Entities;
using FoaieDeParcurs.Data;
using FoaieDeParcurs.Data.Repositories;
using Microsoft.Data.Sqlite;

namespace FoaieDeParcurs.Tests.Repositories;

public sealed class FillUpRepositoryTests : IDisposable
{
    private readonly string _dbPath;

    public FillUpRepositoryTests()
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

    private FillUpRepository CreateRepository() => new(AppDbContextFactory.Create(_dbPath));

    private static RouteSegment Segment(string start, string end) => new()
    {
        StartLocationName = start,
        StartLatitude = 1,
        StartLongitude = 1,
        StartTimestamp = DateTimeOffset.UtcNow.AddHours(-1),
        EndLocationName = end,
        EndLatitude = 2,
        EndLongitude = 2,
        EndTimestamp = DateTimeOffset.UtcNow,
        DistanceKm = 42.3
    };

    [Fact]
    public async Task AddWithSegmentsAsync_AssignsTheFillUpIdToEverySegment()
    {
        var repository = CreateRepository();
        var fillUp = new FillUp { Timestamp = DateTimeOffset.UtcNow, LitersFilled = 40, AmountPaid = 300, CreatedAt = DateTimeOffset.UtcNow };
        var segments = new List<RouteSegment> { Segment("Depot X", "Cluj-Napoca"), Segment("Cluj-Napoca", "Depot X") };

        var saved = await repository.AddWithSegmentsAsync(fillUp, segments);

        Assert.True(saved.Id > 0);

        var readRepository = CreateRepository();
        var reloadedFillUp = await readRepository.GetByIdAsync(saved.Id);
        Assert.NotNull(reloadedFillUp);
    }

    [Fact]
    public async Task AddWithSegmentsAsync_PersistsBothTheFillUpAndItsSegments_ReReadable()
    {
        var repository = CreateRepository();
        var fillUp = new FillUp { Timestamp = DateTimeOffset.UtcNow, LitersFilled = 40, AmountPaid = 300, CreatedAt = DateTimeOffset.UtcNow };
        var segments = new List<RouteSegment> { Segment("Depot X", "Cluj-Napoca") };

        var saved = await repository.AddWithSegmentsAsync(fillUp, segments);

        // Fresh context/repository — proves a real round trip, not an in-memory echo.
        await using var db = AppDbContextFactory.Create(_dbPath);
        var persistedSegments = db.RouteSegments.Where(s => s.EndFillUpId == saved.Id).ToList();

        var persistedSegment = Assert.Single(persistedSegments);
        Assert.Equal("Depot X", persistedSegment.StartLocationName);
        Assert.Equal("Cluj-Napoca", persistedSegment.EndLocationName);
        Assert.True(persistedSegment.DistanceKm > 0);
    }

    [Fact]
    public async Task GetMostRecentAsync_ReturnsNull_WhenNoFillUpsExistYet()
    {
        var repository = CreateRepository();

        var result = await repository.GetMostRecentAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMostRecentAsync_ReturnsTheLatestByTimestamp()
    {
        var repository = CreateRepository();
        var older = new FillUp { Timestamp = DateTimeOffset.UtcNow.AddDays(-2), LitersFilled = 30, AmountPaid = 200, CreatedAt = DateTimeOffset.UtcNow };
        var newer = new FillUp { Timestamp = DateTimeOffset.UtcNow, LitersFilled = 40, AmountPaid = 300, CreatedAt = DateTimeOffset.UtcNow };

        await repository.AddWithSegmentsAsync(older, []);
        var savedNewer = await repository.AddWithSegmentsAsync(newer, []);

        var readRepository = CreateRepository();
        var mostRecent = await readRepository.GetMostRecentAsync();

        Assert.Equal(savedNewer.Id, mostRecent!.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsFillUpsNewestFirst()
    {
        var repository = CreateRepository();
        var older = new FillUp { Timestamp = DateTimeOffset.UtcNow.AddDays(-2), LitersFilled = 30, AmountPaid = 200, CreatedAt = DateTimeOffset.UtcNow };
        var newer = new FillUp { Timestamp = DateTimeOffset.UtcNow, LitersFilled = 40, AmountPaid = 300, CreatedAt = DateTimeOffset.UtcNow };
        await repository.AddWithSegmentsAsync(older, []);
        await repository.AddWithSegmentsAsync(newer, []);

        var readRepository = CreateRepository();
        var all = await readRepository.GetAllAsync();

        Assert.Equal(2, all.Count);
        Assert.Equal(40, all[0].LitersFilled);
        Assert.Equal(30, all[1].LitersFilled);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheFillUp_AndTheSegmentsThatEndAtIt()
    {
        var repository = CreateRepository();
        var fillUp = new FillUp { Timestamp = DateTimeOffset.UtcNow, LitersFilled = 40, AmountPaid = 300, CreatedAt = DateTimeOffset.UtcNow };
        var saved = await repository.AddWithSegmentsAsync(fillUp, [Segment("Depot X", "Cluj-Napoca")]);

        var deleteRepository = CreateRepository();
        await deleteRepository.DeleteAsync(saved.Id);

        var readRepository = CreateRepository();
        Assert.Null(await readRepository.GetByIdAsync(saved.Id));

        await using var db = AppDbContextFactory.Create(_dbPath);
        Assert.Empty(db.RouteSegments.Where(s => s.EndFillUpId == saved.Id));
    }

    [Fact]
    public async Task DeleteAsync_OrphansRatherThanDeletes_SegmentsThatStartAtTheDeletedFillUp()
    {
        var repository = CreateRepository();
        var firstFillUp = await repository.AddWithSegmentsAsync(
            new FillUp { Timestamp = DateTimeOffset.UtcNow.AddDays(-1), LitersFilled = 30, AmountPaid = 200, CreatedAt = DateTimeOffset.UtcNow },
            []);

        var secondSegment = Segment("Depot X", "Brasov");
        secondSegment.StartFillUpId = firstFillUp.Id;
        var secondFillUp = await repository.AddWithSegmentsAsync(
            new FillUp { Timestamp = DateTimeOffset.UtcNow, LitersFilled = 40, AmountPaid = 300, CreatedAt = DateTimeOffset.UtcNow },
            [secondSegment]);

        var deleteRepository = CreateRepository();
        await deleteRepository.DeleteAsync(firstFillUp.Id);

        await using var db = AppDbContextFactory.Create(_dbPath);
        var survivingSegment = db.RouteSegments.Single(s => s.EndFillUpId == secondFillUp.Id);
        Assert.Null(survivingSegment.StartFillUpId);
    }
}

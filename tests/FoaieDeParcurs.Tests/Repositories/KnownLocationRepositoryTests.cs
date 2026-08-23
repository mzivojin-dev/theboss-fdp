using FoaieDeParcurs.Core.Entities;
using FoaieDeParcurs.Data;
using FoaieDeParcurs.Data.Repositories;
using Microsoft.Data.Sqlite;

namespace FoaieDeParcurs.Tests.Repositories;

/// <summary>
/// CRUD against a real (temp-file) SQLite database — no mocking of EF Core or the file system,
/// per the testing decisions in the spec.
/// </summary>
public sealed class KnownLocationRepositoryTests : IDisposable
{
    private readonly string _dbPath;

    public KnownLocationRepositoryTests()
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

    private KnownLocationRepository CreateRepository() =>
        new(AppDbContextFactory.Create(_dbPath));

    [Fact]
    public async Task AddAsync_PersistsAndAssignsId()
    {
        var repository = CreateRepository();

        var location = new KnownLocation
        {
            Name = "Depot X",
            Latitude = 44.4268,
            Longitude = 26.1025,
            RadiusMeters = 150,
            Type = KnownLocationType.Work,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var added = await repository.AddAsync(location);

        Assert.True(added.Id > 0);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryPersistedLocation_AfterReopeningTheDatabase()
    {
        var firstRepository = CreateRepository();
        await firstRepository.AddAsync(new KnownLocation { Name = "Home", Latitude = 1, Longitude = 1, Type = KnownLocationType.Home, CreatedAt = DateTimeOffset.UtcNow });
        await firstRepository.AddAsync(new KnownLocation { Name = "Work", Latitude = 2, Longitude = 2, Type = KnownLocationType.Work, CreatedAt = DateTimeOffset.UtcNow });

        // Fresh repository/context — proves this round-trips through SQLite, not an in-memory cache.
        var secondRepository = CreateRepository();
        var all = await secondRepository.GetAllAsync();

        Assert.Equal(2, all.Count);
        Assert.Contains(all, l => l.Name == "Home");
        Assert.Contains(all, l => l.Name == "Work");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNoSuchLocation()
    {
        var repository = CreateRepository();

        var result = await repository.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var addRepository = CreateRepository();
        var added = await addRepository.AddAsync(new KnownLocation
        {
            Name = "Depot X",
            Latitude = 44.4268,
            Longitude = 26.1025,
            RadiusMeters = 150,
            Type = KnownLocationType.Work,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var editRepository = CreateRepository();
        var toEdit = await editRepository.GetByIdAsync(added.Id);
        toEdit!.Name = "Depot X (renamed)";
        toEdit.RadiusMeters = 200;
        await editRepository.UpdateAsync(toEdit);

        var readRepository = CreateRepository();
        var reloaded = await readRepository.GetByIdAsync(added.Id);

        Assert.Equal("Depot X (renamed)", reloaded!.Name);
        Assert.Equal(200, reloaded.RadiusMeters);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges_WhenReusingTheSameRepositoryInstanceAsAdd()
    {
        // Regression test: the app resolves AppDbContext once for the whole session (MAUI never
        // creates a DI scope per page), so a repository's AddAsync and a later UpdateAsync run
        // against the SAME DbContext, not fresh ones like CreateRepository() gives every other
        // test in this file. AddAsync tracks the entity it inserts; UpdateAsync used to build a
        // brand-new instance for the same Id, which EF Core rejects with "already tracked" —
        // an unhandled crash. See ChangeTrackerExtensions.DetachStaleTrackedInstance.
        var repository = CreateRepository();
        var added = await repository.AddAsync(new KnownLocation
        {
            Name = "Depot X",
            Latitude = 44.4268,
            Longitude = 26.1025,
            RadiusMeters = 150,
            Type = KnownLocationType.Work,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await repository.UpdateAsync(new KnownLocation
        {
            Id = added.Id,
            Name = "Depot X (renamed)",
            Latitude = 44.4268,
            Longitude = 26.1025,
            RadiusMeters = 200,
            Type = KnownLocationType.Work,
            CreatedAt = added.CreatedAt
        });

        var reloaded = await repository.GetByIdAsync(added.Id);
        Assert.Equal("Depot X (renamed)", reloaded!.Name);
        Assert.Equal(200, reloaded.RadiusMeters);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheLocation()
    {
        var addRepository = CreateRepository();
        var added = await addRepository.AddAsync(new KnownLocation
        {
            Name = "Depot X",
            Latitude = 44.4268,
            Longitude = 26.1025,
            Type = KnownLocationType.Work,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var deleteRepository = CreateRepository();
        await deleteRepository.DeleteAsync(added.Id);

        var readRepository = CreateRepository();
        var reloaded = await readRepository.GetByIdAsync(added.Id);

        Assert.Null(reloaded);
    }
}

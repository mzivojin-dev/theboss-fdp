using FoaieDeParcurs.Core.Entities;
using FoaieDeParcurs.Data;
using FoaieDeParcurs.Data.Repositories;
using Microsoft.Data.Sqlite;

namespace FoaieDeParcurs.Tests.Repositories;

public sealed class VehicleProfileRepositoryTests : IDisposable
{
    private readonly string _dbPath;

    public VehicleProfileRepositoryTests()
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

    private VehicleProfileRepository CreateRepository() =>
        new(AppDbContextFactory.Create(_dbPath));

    [Fact]
    public async Task GetOrCreateAsync_CreatesADefaultProfile_WhenNoneExistsYet()
    {
        var repository = CreateRepository();

        var profile = await repository.GetOrCreateAsync();

        Assert.True(profile.Id > 0);
        Assert.Equal(ReportingCadence.PerFillUp, profile.ReportingCadence);
    }

    [Fact]
    public async Task GetOrCreateAsync_ReturnsTheSameProfile_OnSubsequentCalls()
    {
        var firstRepository = CreateRepository();
        var first = await firstRepository.GetOrCreateAsync();

        var secondRepository = CreateRepository();
        var second = await secondRepository.GetOrCreateAsync();

        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task SaveAsync_RoundTripsEveryField()
    {
        var setupRepository = CreateRepository();
        var profile = await setupRepository.GetOrCreateAsync();

        profile.CompanyName = "Acme SRL";
        profile.Cui = "RO12345678";
        profile.DriverName = "Mihai Zivojinovic";
        profile.VehiclePlate = "B-01-ABC";
        profile.VehicleMakeModel = "Dacia Duster";
        profile.VehicleCategory = "M1";
        profile.FuelType = FuelType.Motorina;
        profile.FuelConsumptionNormPer100Km = 6.8;
        profile.EmailRecipient = "contabilitate@acme.ro";
        profile.EmailSubjectTemplate = "Custom subject {PeriodStart}";
        profile.EmailBodyTemplate = "Custom body";
        profile.ReportingCadence = ReportingCadence.Monthly;

        await setupRepository.SaveAsync(profile);

        var readRepository = CreateRepository();
        var reloaded = await readRepository.GetOrCreateAsync();

        Assert.Equal("Acme SRL", reloaded.CompanyName);
        Assert.Equal("RO12345678", reloaded.Cui);
        Assert.Equal("Mihai Zivojinovic", reloaded.DriverName);
        Assert.Equal("B-01-ABC", reloaded.VehiclePlate);
        Assert.Equal("Dacia Duster", reloaded.VehicleMakeModel);
        Assert.Equal("M1", reloaded.VehicleCategory);
        Assert.Equal(FuelType.Motorina, reloaded.FuelType);
        Assert.Equal(6.8, reloaded.FuelConsumptionNormPer100Km);
        Assert.Equal("contabilitate@acme.ro", reloaded.EmailRecipient);
        Assert.Equal("Custom subject {PeriodStart}", reloaded.EmailSubjectTemplate);
        Assert.Equal("Custom body", reloaded.EmailBodyTemplate);
        Assert.Equal(ReportingCadence.Monthly, reloaded.ReportingCadence);
    }

    [Fact]
    public async Task SaveAsync_Succeeds_WhenReusingTheSameRepositoryInstanceAsGetOrCreate()
    {
        // Regression test: the app resolves AppDbContext once for the whole session (MAUI never
        // creates a DI scope per page), so SettingsViewModel's LoadAsync (GetOrCreateAsync) and
        // SaveAsync run against the SAME DbContext, not fresh ones like CreateRepository() gives
        // every other test in this file. GetOrCreateAsync tracks the profile it creates on first
        // launch; SaveAsync built a brand-new VehicleProfile instance for the same Id, which EF
        // Core rejects with "already tracked" — this was the Settings "Save" crash reported by
        // the user. See ChangeTrackerExtensions.DetachStaleTrackedInstance.
        var repository = CreateRepository();
        var profile = await repository.GetOrCreateAsync();

        await repository.SaveAsync(new VehicleProfile { Id = profile.Id, CompanyName = "First Save" });
        await repository.SaveAsync(new VehicleProfile { Id = profile.Id, CompanyName = "Second Save" });

        var reloaded = await repository.GetOrCreateAsync();
        Assert.Equal("Second Save", reloaded.CompanyName);
    }
}

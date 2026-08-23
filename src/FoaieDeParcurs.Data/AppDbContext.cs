using FoaieDeParcurs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoaieDeParcurs.Data;

/// <summary>
/// The single EF Core context for the app. Core entities are mapped entirely through
/// <see cref="OnModelCreating"/> below — no persistence attributes live on the entities
/// themselves, so FoaieDeParcurs.Core stays free of any dependency on EF Core or SQLite.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<KnownLocation> KnownLocations => Set<KnownLocation>();
    public DbSet<FillUp> FillUps => Set<FillUp>();
    public DbSet<RouteSegment> RouteSegments => Set<RouteSegment>();
    public DbSet<GpsRawPoint> GpsRawPoints => Set<GpsRawPoint>();
    public DbSet<VehicleProfile> VehicleProfiles => Set<VehicleProfile>();

    /// <summary>
    /// SQLite's EF Core provider can't translate DateTimeOffset comparisons/ordering
    /// server-side. Every DateTimeOffset in this app is always UTC, so storing as a plain
    /// UTC DateTime (which SQLite compares/orders natively) loses nothing and fixes queries
    /// like "points at or after this timestamp".
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToUtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KnownLocation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
        });

        modelBuilder.Entity<FillUp>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AmountPaid).HasPrecision(18, 2);
            e.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        });

        modelBuilder.Entity<RouteSegment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Purpose).IsRequired();
        });

        modelBuilder.Entity<GpsRawPoint>(e =>
        {
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<VehicleProfile>(e =>
        {
            e.HasKey(x => x.Id);
        });
    }

    /// <summary>Applies any pending migrations, creating the database file/schema if needed.</summary>
    public Task InitializeAsync() => Database.MigrateAsync();
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FoaieDeParcurs.Data;

/// <summary>Lets `dotnet ef migrations` construct <see cref="AppDbContext"/> without a running app host.</summary>
public sealed class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=design-time.db3")
            .Options;

        return new AppDbContext(options);
    }
}

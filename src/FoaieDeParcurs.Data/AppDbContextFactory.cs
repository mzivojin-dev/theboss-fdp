using Microsoft.EntityFrameworkCore;

namespace FoaieDeParcurs.Data;

/// <summary>Builds <see cref="AppDbContext"/> instances pointed at a given SQLite file path.</summary>
public static class AppDbContextFactory
{
    public static AppDbContext Create(string databasePath)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        return new AppDbContext(options);
    }
}

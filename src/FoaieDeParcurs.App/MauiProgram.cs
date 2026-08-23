using FoaieDeParcurs.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FoaieDeParcurs.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		var databasePath = Path.Combine(FileSystem.AppDataDirectory, "foaiedeparcurs.db3");
		builder.Services.AddDbContext<AppDbContext>(options =>
			options.UseSqlite($"Data Source={databasePath}"));

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		using (var scope = app.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			db.InitializeAsync().GetAwaiter().GetResult();
		}

		return app;
	}
}

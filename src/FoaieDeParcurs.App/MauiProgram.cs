using FoaieDeParcurs.App.ViewModels.KnownLocations;
using FoaieDeParcurs.App.ViewModels.Settings;
using FoaieDeParcurs.App.Views.KnownLocations;
using FoaieDeParcurs.App.Views.Settings;
using FoaieDeParcurs.Core.Repositories;
using FoaieDeParcurs.Data;
using FoaieDeParcurs.Data.Repositories;
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
			.UseMauiMaps()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		var databasePath = Path.Combine(FileSystem.AppDataDirectory, "foaiedeparcurs.db3");
		builder.Services.AddDbContext<AppDbContext>(options =>
			options.UseSqlite($"Data Source={databasePath}"));

		builder.Services.AddScoped<IKnownLocationRepository, KnownLocationRepository>();
		builder.Services.AddScoped<IVehicleProfileRepository, VehicleProfileRepository>();

		builder.Services.AddTransient<KnownLocationListViewModel>();
		builder.Services.AddTransient<KnownLocationListPage>();
		builder.Services.AddTransient<KnownLocationEditViewModel>();
		builder.Services.AddTransient<KnownLocationEditPage>();

		builder.Services.AddTransient<SettingsViewModel>();
		builder.Services.AddTransient<SettingsPage>();

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

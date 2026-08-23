using System.Globalization;
using FoaieDeParcurs.App.Platforms.Android;
using FoaieDeParcurs.App.Services;
using FoaieDeParcurs.App.ViewModels.Dashboard;
using FoaieDeParcurs.App.ViewModels.FillUps;
using FoaieDeParcurs.App.ViewModels.KnownLocations;
using FoaieDeParcurs.App.ViewModels.Settings;
using FoaieDeParcurs.App.Views.Dashboard;
using FoaieDeParcurs.App.Views.FillUps;
using FoaieDeParcurs.App.Views.KnownLocations;
using FoaieDeParcurs.App.Views.Settings;
using FoaieDeParcurs.Core.Domain;
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
		// Single-user app for a Romanian company driver (see spec) — the UI, dates, and number
		// formatting are Romanian throughout, not switchable. Set before anything else runs so
		// every subsequently-created thread (including the UI thread) inherits it.
		var romanianCulture = new CultureInfo("ro-RO");
		CultureInfo.DefaultThreadCurrentCulture = romanianCulture;
		CultureInfo.DefaultThreadCurrentUICulture = romanianCulture;

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

		builder.Services.AddScoped<IKnownLocationRepository, KnownLocationRepository>();
		builder.Services.AddScoped<IVehicleProfileRepository, VehicleProfileRepository>();
		builder.Services.AddScoped<IGpsRawPointRepository, GpsRawPointRepository>();
		builder.Services.AddScoped<IFillUpRepository, FillUpRepository>();
		builder.Services.AddScoped<IRouteSegmentRepository, RouteSegmentRepository>();
		builder.Services.AddSingleton<ILocationNamer, RomanianCityGazetteer>();
		builder.Services.AddSingleton<ITrackingService, AndroidTrackingService>();
		builder.Services.AddScoped<IFoaieDeParcursDocumentService, FoaieDeParcursDocumentService>();

		builder.Services.AddTransient<KnownLocationListViewModel>();
		builder.Services.AddTransient<KnownLocationListPage>();
		builder.Services.AddTransient<KnownLocationEditViewModel>();
		builder.Services.AddTransient<KnownLocationEditPage>();

		builder.Services.AddTransient<SettingsViewModel>();
		builder.Services.AddTransient<SettingsPage>();

		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<FillUpCaptureViewModel>();
		builder.Services.AddTransient<FillUpCapturePage>();

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

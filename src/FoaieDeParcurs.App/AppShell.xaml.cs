using FoaieDeParcurs.App.Views.KnownLocations;

namespace FoaieDeParcurs.App;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(KnownLocationEditPage), typeof(KnownLocationEditPage));
	}
}

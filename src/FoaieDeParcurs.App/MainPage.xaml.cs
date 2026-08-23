using FoaieDeParcurs.App.ViewModels;

namespace FoaieDeParcurs.App;

public partial class MainPage : ContentPage
{
	private readonly TrackingViewModel _viewModel;

	public MainPage(TrackingViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	private void OnTrackingToggled(object? sender, ToggledEventArgs e) =>
		_viewModel.ToggleCommand.Execute(null);
}

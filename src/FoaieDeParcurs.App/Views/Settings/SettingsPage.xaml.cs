using FoaieDeParcurs.App.ViewModels.Settings;

namespace FoaieDeParcurs.App.Views.Settings;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadCommand.Execute(null);
    }

    private void OnTrackingToggled(object? sender, ToggledEventArgs e) =>
        _viewModel.ToggleTrackingCommand.Execute(null);
}

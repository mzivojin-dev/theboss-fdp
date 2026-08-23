using FoaieDeParcurs.App.ViewModels.KnownLocations;

namespace FoaieDeParcurs.App.Views.KnownLocations;

public partial class KnownLocationListPage : ContentPage
{
    private readonly KnownLocationListViewModel _viewModel;

    public KnownLocationListPage(KnownLocationListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadCommand.Execute(null);
    }
}

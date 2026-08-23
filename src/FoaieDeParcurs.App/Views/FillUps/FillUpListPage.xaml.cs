using FoaieDeParcurs.App.ViewModels.FillUps;

namespace FoaieDeParcurs.App.Views.FillUps;

public partial class FillUpListPage : ContentPage
{
    private readonly FillUpListViewModel _viewModel;

    public FillUpListPage(FillUpListViewModel viewModel)
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

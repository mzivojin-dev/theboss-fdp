using FoaieDeParcurs.App.ViewModels.FillUps;

namespace FoaieDeParcurs.App.Views.FillUps;

public partial class FillUpCapturePage : ContentPage
{
    private readonly FillUpCaptureViewModel _viewModel;

    public FillUpCapturePage(FillUpCaptureViewModel viewModel)
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

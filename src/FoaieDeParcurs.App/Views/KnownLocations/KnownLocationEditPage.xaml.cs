using FoaieDeParcurs.App.ViewModels.KnownLocations;

namespace FoaieDeParcurs.App.Views.KnownLocations;

public partial class KnownLocationEditPage : ContentPage
{
    public KnownLocationEditPage(KnownLocationEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

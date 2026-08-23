using System.ComponentModel;
using FoaieDeParcurs.App.ViewModels.KnownLocations;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace FoaieDeParcurs.App.Views.KnownLocations;

public partial class KnownLocationEditPage : ContentPage
{
    private readonly KnownLocationEditViewModel _viewModel;
    private Pin? _pin;

    public KnownLocationEditPage(KnownLocationEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        LocationMap.MapClicked += OnMapClicked;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MoveMapToCurrentPin();
    }

    private void OnMapClicked(object? sender, MapClickedEventArgs e)
    {
        _viewModel.SetPinLocation(e.Location.Latitude, e.Location.Longitude);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(KnownLocationEditViewModel.Latitude) or nameof(KnownLocationEditViewModel.Longitude))
        {
            MoveMapToCurrentPin();
        }
    }

    private void MoveMapToCurrentPin()
    {
        var location = new Location(_viewModel.Latitude, _viewModel.Longitude);

        if (_pin is not null)
        {
            LocationMap.Pins.Remove(_pin);
        }

        _pin = new Pin
        {
            Location = location,
            Label = string.IsNullOrWhiteSpace(_viewModel.Name) ? "Pin" : _viewModel.Name
        };
        LocationMap.Pins.Add(_pin);

        LocationMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(500)));
    }
}

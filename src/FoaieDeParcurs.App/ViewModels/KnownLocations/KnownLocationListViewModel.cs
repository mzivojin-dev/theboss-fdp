using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoaieDeParcurs.Core.Entities;
using FoaieDeParcurs.Core.Repositories;

namespace FoaieDeParcurs.App.ViewModels.KnownLocations;

public sealed partial class KnownLocationListViewModel(IKnownLocationRepository repository) : ObservableObject
{
    public ObservableCollection<KnownLocation> Locations { get; } = [];

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isEmpty;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsRefreshing = true;
        try
        {
            var all = await repository.GetAllAsync();
            Locations.Clear();
            foreach (var location in all)
            {
                Locations.Add(location);
            }

            IsEmpty = Locations.Count == 0;
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private static async Task AddAsync() =>
        await Shell.Current.GoToAsync(nameof(Views.KnownLocations.KnownLocationEditPage));

    [RelayCommand]
    private static async Task EditAsync(KnownLocation location) =>
        await Shell.Current.GoToAsync(nameof(Views.KnownLocations.KnownLocationEditPage),
            new Dictionary<string, object> { [KnownLocationEditViewModel.LocationIdQueryKey] = location.Id });

    [RelayCommand]
    private async Task DeleteAsync(KnownLocation location)
    {
        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Ștergere locație",
            $"Ștergeți „{location.Name}”?",
            "Șterge",
            "Anulează");

        if (!confirmed)
        {
            return;
        }

        await repository.DeleteAsync(location.Id);
        Locations.Remove(location);
        IsEmpty = Locations.Count == 0;
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoaieDeParcurs.Core.Entities;
using FoaieDeParcurs.Core.Repositories;

namespace FoaieDeParcurs.App.ViewModels.FillUps;

/// <summary>Backs the Home screen: the chronological fill-up list.</summary>
public sealed partial class FillUpListViewModel(IFillUpRepository repository) : ObservableObject
{
    public ObservableCollection<FillUp> FillUps { get; } = [];

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
            FillUps.Clear();
            foreach (var fillUp in all)
            {
                FillUps.Add(fillUp);
            }

            IsEmpty = FillUps.Count == 0;
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private static async Task AddAsync() =>
        await Shell.Current.GoToAsync(nameof(Views.FillUps.FillUpCapturePage));

    [RelayCommand]
    private static async Task ViewAsync(FillUp fillUp) =>
        await Shell.Current.GoToAsync(nameof(Views.FillUps.FillUpCapturePage),
            new Dictionary<string, object> { [FillUpCaptureViewModel.FillUpIdQueryKey] = fillUp.Id });

    [RelayCommand]
    private async Task DeleteAsync(FillUp fillUp)
    {
        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Delete fill-up",
            $"Delete the fill-up from {fillUp.Timestamp.LocalDateTime:g}? Its route segments will be removed too.",
            "Delete",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        await repository.DeleteAsync(fillUp.Id);
        FillUps.Remove(fillUp);
        IsEmpty = FillUps.Count == 0;
    }
}

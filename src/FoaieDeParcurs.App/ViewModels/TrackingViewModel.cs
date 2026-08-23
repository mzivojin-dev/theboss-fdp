using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoaieDeParcurs.App.Services;

namespace FoaieDeParcurs.App.ViewModels;

/// <summary>
/// Manual override for the background GPS tracker (interim Home-tab control — ticket #6
/// replaces this tab with the real Fill-up list; tracking itself runs automatically once
/// started, adapting its own rate, so this is mostly a permission-grant + visibility surface).
/// </summary>
public sealed partial class TrackingViewModel(ITrackingService trackingService) : ObservableObject
{
    [ObservableProperty]
    private bool _isTracking = trackingService.IsTracking;

    [ObservableProperty]
    private string? _statusMessage;

    [RelayCommand]
    private async Task ToggleAsync()
    {
        if (trackingService.IsTracking)
        {
            trackingService.Stop();
            IsTracking = false;
            StatusMessage = "Tracking stopped.";
            return;
        }

        var granted = await trackingService.RequestPermissionsAsync();
        if (!granted)
        {
            StatusMessage = "Location and notification permissions are required to track driving.";
            return;
        }

        trackingService.Start();
        IsTracking = true;
        StatusMessage = "Tracking started — a persistent notification shows while it's active.";
    }
}

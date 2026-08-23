using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoaieDeParcurs.App.Services;
using FoaieDeParcurs.Core.Entities;
using FoaieDeParcurs.Core.Repositories;

namespace FoaieDeParcurs.App.ViewModels.Settings;

/// <summary>
/// Backs the one-time company/vehicle/driver Settings screen (spec stories 44-47), plus the
/// manual override for background GPS tracking. The underlying <see cref="VehicleProfile"/>
/// is a singleton row created on first load.
/// </summary>
public sealed partial class SettingsViewModel(IVehicleProfileRepository repository, ITrackingService trackingService) : ObservableObject
{
    private int _profileId;

    [ObservableProperty]
    private bool _isTracking = trackingService.IsTracking;

    [ObservableProperty]
    private string? _trackingStatusMessage;

    [ObservableProperty]
    private string _companyName = string.Empty;

    [ObservableProperty]
    private string _cui = string.Empty;

    [ObservableProperty]
    private string _driverName = string.Empty;

    [ObservableProperty]
    private string _vehiclePlate = string.Empty;

    [ObservableProperty]
    private string _vehicleMakeModel = string.Empty;

    [ObservableProperty]
    private string _vehicleCategory = string.Empty;

    [ObservableProperty]
    private FuelType _fuelType = FuelType.Benzina;

    [ObservableProperty]
    private double _fuelConsumptionNormPer100Km;

    [ObservableProperty]
    private string _googleMapsApiKey = string.Empty;

    [ObservableProperty]
    private string _emailRecipient = string.Empty;

    [ObservableProperty]
    private string _emailSubjectTemplate = string.Empty;

    [ObservableProperty]
    private string _emailBodyTemplate = string.Empty;

    [ObservableProperty]
    private ReportingCadence _reportingCadence = ReportingCadence.PerFillUp;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    public IReadOnlyList<FuelType> AvailableFuelTypes { get; } = Enum.GetValues<FuelType>();
    public IReadOnlyList<ReportingCadence> AvailableCadences { get; } = Enum.GetValues<ReportingCadence>();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var profile = await repository.GetOrCreateAsync();
            _profileId = profile.Id;
            CompanyName = profile.CompanyName;
            Cui = profile.Cui;
            DriverName = profile.DriverName;
            VehiclePlate = profile.VehiclePlate;
            VehicleMakeModel = profile.VehicleMakeModel;
            VehicleCategory = profile.VehicleCategory;
            FuelType = profile.FuelType;
            FuelConsumptionNormPer100Km = profile.FuelConsumptionNormPer100Km;
            GoogleMapsApiKey = profile.GoogleMapsApiKey;
            EmailRecipient = profile.EmailRecipient;
            EmailSubjectTemplate = profile.EmailSubjectTemplate;
            EmailBodyTemplate = profile.EmailBodyTemplate;
            ReportingCadence = profile.ReportingCadence;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsBusy = true;
        StatusMessage = null;
        try
        {
            await repository.SaveAsync(new VehicleProfile
            {
                Id = _profileId,
                CompanyName = CompanyName,
                Cui = Cui,
                DriverName = DriverName,
                VehiclePlate = VehiclePlate,
                VehicleMakeModel = VehicleMakeModel,
                VehicleCategory = VehicleCategory,
                FuelType = FuelType,
                FuelConsumptionNormPer100Km = FuelConsumptionNormPer100Km,
                GoogleMapsApiKey = GoogleMapsApiKey,
                EmailRecipient = EmailRecipient,
                EmailSubjectTemplate = EmailSubjectTemplate,
                EmailBodyTemplate = EmailBodyTemplate,
                ReportingCadence = ReportingCadence
            });

            StatusMessage = "Saved.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleTrackingAsync()
    {
        if (trackingService.IsTracking)
        {
            trackingService.Stop();
            IsTracking = false;
            TrackingStatusMessage = "Tracking stopped.";
            return;
        }

        var granted = await trackingService.RequestPermissionsAsync();
        if (!granted)
        {
            TrackingStatusMessage = "Location and notification permissions are required to track driving.";
            return;
        }

        trackingService.Start();
        IsTracking = true;
        TrackingStatusMessage = "Tracking started — a persistent notification shows while it's active.";
    }
}

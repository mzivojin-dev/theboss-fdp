using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoaieDeParcurs.Core.Entities;
using FoaieDeParcurs.Core.Repositories;

namespace FoaieDeParcurs.App.ViewModels.KnownLocations;

/// <summary>
/// Backs the add/edit screen for a single Known Location. Navigated to with no query
/// parameters to add a new one, or with <see cref="LocationIdQueryKey"/> set to edit an
/// existing one.
/// </summary>
public sealed partial class KnownLocationEditViewModel(IKnownLocationRepository repository)
    : ObservableObject, IQueryAttributable
{
    public const string LocationIdQueryKey = "locationId";

    /// <summary>Bucharest — a reasonable default map center for a Romanian driver with no pin set yet.</summary>
    private static readonly Microsoft.Maui.Devices.Sensors.Location DefaultCenter = new(44.4268, 26.1025);

    private int? _id;

    [ObservableProperty]
    private string _title = "Locație nouă";

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private double _latitude = DefaultCenter.Latitude;

    [ObservableProperty]
    private double _longitude = DefaultCenter.Longitude;

    [ObservableProperty]
    private double _radiusMeters = 150;

    [ObservableProperty]
    private KnownLocationType _type = KnownLocationType.Custom;

    [ObservableProperty]
    private string _searchAddress = string.Empty;

    [ObservableProperty]
    private bool _isExistingLocation;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    public IReadOnlyList<KnownLocationType> AvailableTypes { get; } =
        Enum.GetValues<KnownLocationType>();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(LocationIdQueryKey, out var value) && value is int id)
        {
            _id = id;
            IsExistingLocation = true;
            Title = "Editare locație";
            _ = LoadAsync(id);
        }
    }

    private async Task LoadAsync(int id)
    {
        IsBusy = true;
        try
        {
            var location = await repository.GetByIdAsync(id);
            if (location is null)
            {
                StatusMessage = "Această locație nu mai există.";
                return;
            }

            Name = location.Name;
            Latitude = location.Latitude;
            Longitude = location.Longitude;
            RadiusMeters = location.RadiusMeters;
            Type = location.Type;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Called from the map's tap gesture (code-behind), since Map has no bindable tap command.</summary>
    public void SetPinLocation(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    [RelayCommand]
    private async Task SearchAddressAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchAddress))
        {
            return;
        }

        IsBusy = true;
        StatusMessage = null;
        try
        {
            // Microsoft.Maui.Devices.Sensors.Geocoding uses the platform's native geocoder
            // (Android's system Geocoder), which does NOT require a Google Maps API key —
            // this keeps address search working even with no key configured.
            var results = (await Geocoding.Default.GetLocationsAsync(SearchAddress)).ToList();
            var match = results.FirstOrDefault();
            if (match is null)
            {
                StatusMessage = "Nu a fost găsită nicio adresă corespunzătoare.";
                return;
            }

            Latitude = match.Latitude;
            Longitude = match.Longitude;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Căutarea adresei nu este disponibilă: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            StatusMessage = "Numele este obligatoriu.";
            return;
        }

        IsBusy = true;
        try
        {
            if (_id is int id)
            {
                var existing = await repository.GetByIdAsync(id);
                if (existing is null)
                {
                    StatusMessage = "Această locație nu mai există.";
                    return;
                }

                existing.Name = Name;
                existing.Latitude = Latitude;
                existing.Longitude = Longitude;
                existing.RadiusMeters = RadiusMeters;
                existing.Type = Type;
                await repository.UpdateAsync(existing);
            }
            else
            {
                await repository.AddAsync(new Core.Entities.KnownLocation
                {
                    Name = Name,
                    Latitude = Latitude,
                    Longitude = Longitude,
                    RadiusMeters = RadiusMeters,
                    Type = Type,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (_id is not int id)
        {
            return;
        }

        var confirmed = await Shell.Current.DisplayAlertAsync("Ștergere locație", $"Ștergeți „{Name}”?", "Șterge", "Anulează");
        if (!confirmed)
        {
            return;
        }

        await repository.DeleteAsync(id);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private static async Task CancelAsync() => await Shell.Current.GoToAsync("..");
}

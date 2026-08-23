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

    /// <summary>Bucharest — a reasonable default for a Romanian driver who hasn't set coordinates yet.</summary>
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

    /// <summary>Drives the status line's colour — the same line reports both failures and successes.</summary>
    [ObservableProperty]
    private bool _isStatusError;

    public IReadOnlyList<KnownLocationType> AvailableTypes { get; } =
        Enum.GetValues<KnownLocationType>();

    private void ReportError(string message)
    {
        IsStatusError = true;
        StatusMessage = message;
    }

    private void ReportSuccess(string message)
    {
        IsStatusError = false;
        StatusMessage = message;
    }

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
                ReportError("Această locație nu mai există.");
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

    [RelayCommand]
    private async Task SearchAddressAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchAddress))
        {
            ReportError("Introduceți o adresă de căutat.");
            return;
        }

        IsBusy = true;
        StatusMessage = null;
        try
        {
            // Microsoft.Maui.Devices.Sensors.Geocoding uses the platform's own geocoder
            // (Android's system Geocoder) — no Google Maps SDK and no API key involved.
            var results = (await Geocoding.Default.GetLocationsAsync(SearchAddress)).ToList();
            var match = results.FirstOrDefault();
            if (match is null)
            {
                ReportError("Nu a fost găsită nicio adresă corespunzătoare. Încercați o adresă mai exactă sau introduceți coordonatele manual.");
                return;
            }

            Latitude = match.Latitude;
            Longitude = match.Longitude;
            ReportSuccess($"Adresă găsită: {match.Latitude:0.00000}, {match.Longitude:0.00000}");
        }
        catch (Exception ex)
        {
            ReportError($"Căutarea adresei nu este disponibilă: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Replaces what tapping the map used to do — the common case is standing at the place you
    /// want to save. Uses the platform's own location services, no Maps SDK.
    /// </summary>
    [RelayCommand]
    private async Task UseCurrentLocationAsync()
    {
        IsBusy = true;
        StatusMessage = null;
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status != PermissionStatus.Granted)
            {
                ReportError("Permisiunea de locație este necesară pentru a folosi poziția curentă.");
                return;
            }

            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(15)));

            if (location is null)
            {
                ReportError("Poziția curentă nu a putut fi determinată. Încercați din nou sau introduceți coordonatele manual.");
                return;
            }

            Latitude = location.Latitude;
            Longitude = location.Longitude;
            ReportSuccess($"Poziția curentă: {location.Latitude:0.00000}, {location.Longitude:0.00000}");
        }
        catch (Exception ex)
        {
            ReportError($"Poziția curentă nu este disponibilă: {ex.Message}");
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
            ReportError("Numele este obligatoriu.");
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
                    ReportError("Această locație nu mai există.");
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

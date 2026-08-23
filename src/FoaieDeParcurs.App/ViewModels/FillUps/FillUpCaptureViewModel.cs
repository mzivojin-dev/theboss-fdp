using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoaieDeParcurs.Core.Domain;
using FoaieDeParcurs.Core.Entities;
using FoaieDeParcurs.Core.Repositories;
using Microsoft.Maui.Devices.Sensors;

namespace FoaieDeParcurs.App.ViewModels.FillUps;

/// <summary>
/// Backs the New/Edit Fill-Up screen: detects the current (station) location, auto-prefills
/// route segments from the GPS trail since the last fill-up via <see cref="TripLedger"/>, and
/// saves everything as one transaction. Viewing a past fill-up loads it read-only.
/// </summary>
public sealed partial class FillUpCaptureViewModel(
    IFillUpRepository fillUpRepository,
    IKnownLocationRepository knownLocationRepository,
    IGpsRawPointRepository gpsRawPointRepository,
    IRouteSegmentRepository routeSegmentRepository,
    ILocationNamer locationNamer)
    : ObservableObject, IQueryAttributable
{
    public const string FillUpIdQueryKey = "fillUpId";

    private int? _existingId;
    private int? _savedFillUpId;
    private FillUp? _previousFillUp;
    private List<KnownLocation> _knownLocations = [];

    [ObservableProperty]
    private string _title = "New Fill-Up";

    [ObservableProperty]
    private bool _canEdit = true;

    [ObservableProperty]
    private string _stationName = string.Empty;

    [ObservableProperty]
    private double _stationLatitude;

    [ObservableProperty]
    private double _stationLongitude;

    [ObservableProperty]
    private KnownLocation? _selectedKnownLocation;

    [ObservableProperty]
    private double _litersFilled;

    [ObservableProperty]
    private decimal _amountPaid;

    [ObservableProperty]
    private string _currency = "RON";

    [ObservableProperty]
    private double? _odometerReading;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string? _receiptPhotoPath;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isVerified;

    public ObservableCollection<KnownLocation> AvailableKnownLocations { get; } = [];
    public ObservableCollection<RouteSegment> Segments { get; } = [];
    public ObservableCollection<string> VerificationIssues { get; } = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(FillUpIdQueryKey, out var value) && value is int id)
        {
            _existingId = id;
            Title = "Fill-Up Details";
            CanEdit = false;
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var knownLocations = await knownLocationRepository.GetAllAsync();
            _knownLocations = knownLocations;
            AvailableKnownLocations.Clear();
            foreach (var location in knownLocations)
            {
                AvailableKnownLocations.Add(location);
            }

            if (_existingId is int id)
            {
                await LoadExistingAsync(id);
            }
            else
            {
                await PrefillNewAsync();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadExistingAsync(int id)
    {
        var fillUp = await fillUpRepository.GetByIdAsync(id);
        if (fillUp is null)
        {
            StatusMessage = "This fill-up no longer exists.";
            return;
        }

        StationName = fillUp.StationName ?? string.Empty;
        StationLatitude = fillUp.StationLatitude ?? 0;
        StationLongitude = fillUp.StationLongitude ?? 0;
        SelectedKnownLocation = fillUp.StationLocationId is int locationId
            ? _knownLocations.FirstOrDefault(l => l.Id == locationId)
            : null;
        LitersFilled = fillUp.LitersFilled;
        AmountPaid = fillUp.AmountPaid;
        Currency = fillUp.Currency;
        OdometerReading = fillUp.OdometerReading;
        Notes = fillUp.Notes ?? string.Empty;
        ReceiptPhotoPath = fillUp.ReceiptPhotoPath;

        var segments = await routeSegmentRepository.GetForFillUpAsync(id);
        Segments.Clear();
        foreach (var segment in segments)
        {
            Segments.Add(segment);
        }
    }

    private async Task PrefillNewAsync()
    {
        await DetectCurrentLocationAsync();

        var previousFillUp = await fillUpRepository.GetMostRecentAsync();
        _previousFillUp = previousFillUp;
        var since = previousFillUp?.Timestamp ?? DateTimeOffset.MinValue;
        var points = await gpsRawPointRepository.GetSinceAsync(since);

        var derived = TripLedger.DeriveRouteSegments(points, _knownLocations, locationNamer, previousFillUp?.Id, null);
        Segments.Clear();
        foreach (var segment in derived)
        {
            Segments.Add(segment);
        }

        if (Segments.Count == 0)
        {
            StatusMessage = points.Count == 0
                ? "No GPS trail recorded since the last fill-up — start tracking from Settings before you drive."
                : "No distinct route segments were detected in the recorded trail.";
        }
    }

    private async Task DetectCurrentLocationAsync()
    {
        try
        {
            var location = await Geolocation.Default.GetLastKnownLocationAsync()
                ?? await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8)));

            if (location is null)
            {
                return;
            }

            StationLatitude = location.Latitude;
            StationLongitude = location.Longitude;

            var knownMatch = FindKnownLocationMatch(location.Latitude, location.Longitude);
            if (knownMatch is not null)
            {
                SelectedKnownLocation = knownMatch;
                StationName = knownMatch.Name;
                return;
            }

            StationName = await ResolveAddressAsync(location.Latitude, location.Longitude);
        }
        catch (Exception ex) when (ex is FeatureNotEnabledException or FeatureNotSupportedException or PermissionException)
        {
            StatusMessage = "Couldn't detect your location automatically — enter the station manually.";
        }
    }

    private KnownLocation? FindKnownLocationMatch(double latitude, double longitude)
    {
        KnownLocation? nearest = null;
        var nearestDistance = double.MaxValue;

        foreach (var location in _knownLocations)
        {
            var distance = GeoMath.HaversineDistanceMeters(latitude, longitude, location.Latitude, location.Longitude);
            if (distance <= location.RadiusMeters && distance < nearestDistance)
            {
                nearest = location;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private async Task<string> ResolveAddressAsync(double latitude, double longitude)
    {
        try
        {
            var placemarks = (await Geocoding.Default.GetPlacemarksAsync(latitude, longitude)).ToList();
            var placemark = placemarks.FirstOrDefault();
            if (placemark is not null)
            {
                var parts = new[] { placemark.Thoroughfare, placemark.Locality }
                    .Where(p => !string.IsNullOrWhiteSpace(p));
                var address = string.Join(", ", parts);
                if (!string.IsNullOrWhiteSpace(address))
                {
                    return address;
                }
            }
        }
        catch
        {
            // Live geocoding needs network; fall through to the offline gazetteer below.
        }

        return locationNamer.ResolveName(latitude, longitude);
    }

    partial void OnSelectedKnownLocationChanged(KnownLocation? value)
    {
        if (value is null)
        {
            return;
        }

        StationName = value.Name;
        StationLatitude = value.Latitude;
        StationLongitude = value.Longitude;
    }

    [RelayCommand]
    private async Task TakePhotoAsync()
    {
        if (!MediaPicker.Default.IsCaptureSupported)
        {
            StatusMessage = "Camera capture isn't supported on this device.";
            return;
        }

        var photo = await MediaPicker.Default.CapturePhotoAsync();
        if (photo is not null)
        {
            ReceiptPhotoPath = await SavePhotoLocallyAsync(photo);
        }
    }

    [RelayCommand]
    private async Task PickPhotoAsync()
    {
        var photo = await MediaPicker.Default.PickPhotoAsync();
        if (photo is not null)
        {
            ReceiptPhotoPath = await SavePhotoLocallyAsync(photo);
        }
    }

    private static async Task<string> SavePhotoLocallyAsync(FileResult photo)
    {
        var receiptsDirectory = Path.Combine(FileSystem.AppDataDirectory, "receipts");
        Directory.CreateDirectory(receiptsDirectory);

        var destinationPath = Path.Combine(receiptsDirectory, $"{Guid.NewGuid():N}{Path.GetExtension(photo.FileName)}");

        await using var source = await photo.OpenReadAsync();
        await using var destination = File.Create(destinationPath);
        await source.CopyToAsync(destination);

        return destinationPath;
    }

    /// <summary>
    /// Saves, then runs the verification requirement (spec: never silently trust the write).
    /// The first save is an insert; if verification fails, the driver can edit the segments
    /// and save again — that re-save updates the same fill-up rather than duplicating it.
    /// Only when verification passes does the app navigate away and mark it verified.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (LitersFilled <= 0)
        {
            StatusMessage = "Liters filled must be greater than zero.";
            return;
        }

        IsBusy = true;
        StatusMessage = null;
        try
        {
            var now = DateTimeOffset.Now;
            var fillUp = new FillUp
            {
                Id = _savedFillUpId ?? 0,
                Timestamp = now,
                StationLocationId = SelectedKnownLocation?.Id,
                StationName = StationName,
                StationLatitude = StationLatitude,
                StationLongitude = StationLongitude,
                LitersFilled = LitersFilled,
                AmountPaid = AmountPaid,
                Currency = Currency,
                OdometerReading = OdometerReading,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                ReceiptPhotoPath = ReceiptPhotoPath,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var segments = Segments.ToList();

            if (_savedFillUpId is int existingSavedId)
            {
                fillUp.Id = existingSavedId;
                await fillUpRepository.UpdateWithSegmentsAsync(fillUp, segments);
            }
            else
            {
                var saved = await fillUpRepository.AddWithSegmentsAsync(fillUp, segments);
                _savedFillUpId = saved.Id;

                // The trail is now captured in the saved segments' simplified polylines — purge it.
                await gpsRawPointRepository.PurgeUpToAsync(now);
            }

            await VerifyAndReportAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Re-reads the just-saved fill-up and its segments back from SQLite — not just trusting
    /// the write didn't throw — then runs <see cref="FillUpVerifier"/> against real persisted
    /// data. Only sets IsVerified true and navigates away when every check passes.
    /// </summary>
    private async Task VerifyAndReportAsync()
    {
        var id = _savedFillUpId!.Value;
        var reloadedFillUp = await fillUpRepository.GetByIdAsync(id);
        var reloadedSegments = await routeSegmentRepository.GetForFillUpAsync(id);

        VerificationIssues.Clear();

        if (reloadedFillUp is null || reloadedSegments.Count != Segments.Count)
        {
            IsVerified = false;
            VerificationIssues.Add("Could not confirm the save against the database — please try again.");
            return;
        }

        var result = FillUpVerifier.Verify(reloadedFillUp, reloadedSegments, _previousFillUp);
        await fillUpRepository.SetVerifiedAsync(id, result.IsVerified);
        IsVerified = result.IsVerified;

        if (result.IsVerified)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        foreach (var issue in result.Issues)
        {
            VerificationIssues.Add(issue);
        }

        StatusMessage = "Saved, but verification found issues — fix the route segments below and save again.";
    }

    [RelayCommand]
    private static async Task CancelAsync() => await Shell.Current.GoToAsync("..");
}

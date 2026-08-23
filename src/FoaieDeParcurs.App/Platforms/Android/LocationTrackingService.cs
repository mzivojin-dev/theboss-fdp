using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Extensions;
using Android.Gms.Location;
using Android.OS;
using AndroidX.Core.App;
using FoaieDeParcurs.Core.Entities;
using FoaieDeParcurs.Core.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;

namespace FoaieDeParcurs.App.Platforms.Android;

/// <summary>
/// Records the GPS trail while driving, from a foreground service so tracking survives the
/// app being backgrounded. Adaptive rate: polls fast while moving, backs off while stationary,
/// and stops itself after a long enough period of no movement — see spec's battery section.
/// </summary>
[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeLocation)]
public sealed class LocationTrackingService : Service
{
    private const string ChannelId = "location_tracking";
    private const int NotificationId = 1001;

    /// <summary>Sustained speed above this counts as "driving" (spec: ~8 km/h).</summary>
    private const double MovingSpeedKmh = 8.0;

    private static readonly TimeSpan MovingInterval = TimeSpan.FromSeconds(7);
    private static readonly TimeSpan IdleInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan IdleBackoffThreshold = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan AutoStopThreshold = TimeSpan.FromMinutes(15);

    private IFusedLocationProviderClient? _client;
    private LocationCallback? _callback;
    private DateTime _lastMovementUtc = DateTime.UtcNow;
    private bool _isMoving;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        StartForeground(NotificationId, BuildNotification());
        _lastMovementUtc = DateTime.UtcNow;
        StartLocationUpdates(moving: false);
        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        if (_client is not null && _callback is not null)
        {
            _client.RemoveLocationUpdates(_callback);
        }

        base.OnDestroy();
    }

    private void StartLocationUpdates(bool moving)
    {
        _client ??= LocationServices.GetFusedLocationProviderClient(this);
        _isMoving = moving;

        if (_callback is not null)
        {
            _client.RemoveLocationUpdates(_callback);
        }

        var intervalMs = (long)(moving ? MovingInterval : IdleInterval).TotalMilliseconds;
        var request = new LocationRequest.Builder(Priority.PriorityHighAccuracy, intervalMs).Build();

        _callback = new RelayLocationCallback(OnLocationResult);
        _client.RequestLocationUpdates(request, _callback, Looper.MainLooper);
    }

    private void OnLocationResult(LocationResult result)
    {
        var location = result.LastLocation;
        if (location is null)
        {
            return;
        }

        var point = new GpsRawPoint
        {
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Timestamp = DateTimeOffset.UtcNow,
            Speed = location.HasSpeed ? location.Speed : null,
            Accuracy = location.HasAccuracy ? location.Accuracy : null
        };

        var repository = IPlatformApplication.Current?.Services.GetService<IGpsRawPointRepository>();
        if (repository is not null)
        {
            _ = repository.AddAsync(point);
        }

        var speedKmh = (location.HasSpeed ? location.Speed : 0) * 3.6;
        var now = DateTime.UtcNow;

        if (speedKmh > MovingSpeedKmh)
        {
            _lastMovementUtc = now;
            if (!_isMoving)
            {
                StartLocationUpdates(moving: true);
            }

            return;
        }

        var idleFor = now - _lastMovementUtc;

        if (_isMoving && idleFor > IdleBackoffThreshold)
        {
            StartLocationUpdates(moving: false);
        }

        if (idleFor > AutoStopThreshold)
        {
            StopSelf();
        }
    }

    private Notification BuildNotification()
    {
        var manager = (NotificationManager)GetSystemService(NotificationService)!;
        if (manager.GetNotificationChannel(ChannelId) is null)
        {
            var channel = new NotificationChannel(ChannelId, "Location tracking", NotificationImportance.Low)
            {
                Description = "Shown while Foaie de Parcurs is recording your route."
            };
            manager.CreateNotificationChannel(channel);
        }

        return new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("Foaie de Parcurs")
            .SetContentText("Recording your route for the next fill-up log")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetOngoing(true)
            .SetPriority(NotificationCompat.PriorityLow)
            .Build();
    }

    private sealed class RelayLocationCallback(Action<LocationResult> onResult) : LocationCallback
    {
        public override void OnLocationResult(LocationResult result) => onResult(result);
    }
}

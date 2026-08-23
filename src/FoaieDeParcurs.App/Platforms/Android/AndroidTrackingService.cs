using Android.Content;
using FoaieDeParcurs.App.Services;
using Microsoft.Maui.ApplicationModel;

namespace FoaieDeParcurs.App.Platforms.Android;

public sealed class AndroidTrackingService : ITrackingService
{
    public bool IsTracking { get; private set; }

    public async Task<bool> RequestPermissionsAsync()
    {
        var location = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (location != PermissionStatus.Granted)
        {
            return false;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var notifications = await Permissions.RequestAsync<Permissions.PostNotifications>();
            if (notifications != PermissionStatus.Granted)
            {
                return false;
            }
        }

        return true;
    }

    public void Start()
    {
        var context = global::Android.App.Application.Context;
        var intent = new Intent(context, typeof(LocationTrackingService));

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            context.StartForegroundService(intent);
        }
        else
        {
            context.StartService(intent);
        }

        IsTracking = true;
    }

    public void Stop()
    {
        var context = global::Android.App.Application.Context;
        context.StopService(new Intent(context, typeof(LocationTrackingService)));
        IsTracking = false;
    }
}

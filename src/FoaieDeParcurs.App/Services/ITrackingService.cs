namespace FoaieDeParcurs.App.Services;

/// <summary>
/// App-facing control surface for the platform's background GPS tracker. The interface has
/// no Android dependency so it stays swappable per-platform (see README on adding iOS later).
/// </summary>
public interface ITrackingService
{
    bool IsTracking { get; }

    /// <summary>Requests location + notification permissions. Returns false if the user declined either.</summary>
    Task<bool> RequestPermissionsAsync();

    void Start();

    void Stop();
}

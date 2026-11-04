using System.Windows;

namespace SmartViewCompanion.Capture;

public sealed record CaptureBackendStatus(bool IsAvailable, string Message);

public interface IFrameCaptureProvider : IAsyncDisposable
{
    string Name { get; }
    Task<CaptureBackendStatus> CheckAvailabilityAsync(CancellationToken cancellationToken = default);
    Task<System.Windows.Media.Imaging.BitmapSource?> CaptureAsync(Rect desktopRegion, CancellationToken cancellationToken = default);
}

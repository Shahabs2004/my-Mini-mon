using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using SmartViewCompanion.Capture;

namespace SmartViewCompanion;

public partial class MainWindow : Window
{
    private CancellationTokenSource? _previewCts;
    private Task? _previewTask;
    private GdiBitBltCaptureProvider? _provider;
    private Rect? _selectedRegion;
    private long _frameCount;
    private readonly Stopwatch _fpsClock = Stopwatch.StartNew();

    private const uint WdaExcludeFromCapture = 0x00000011;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => TryExcludeWindowFromCapture();
        Closing += (_, _) => StopPreview();
    }

    private void TryExcludeWindowFromCapture()
    {
        try
        {
            var handle = new WindowInteropHelper(this).Handle;
            bool applied = SetWindowDisplayAffinity(handle, WdaExcludeFromCapture);
            CaptureExclusionText.Text = applied
                ? "Main window exclusion: requested"
                : "Main window exclusion: unavailable on this Windows configuration";
        }
        catch
        {
            CaptureExclusionText.Text = "Main window exclusion: unavailable";
        }
    }

    private async void SelectRegionButton_Click(object sender, RoutedEventArgs e)
    {
        StopPreview();
        Hide();
        await Task.Delay(180);

        try
        {
            var selector = new RegionSelectionWindow();
            bool? accepted = selector.ShowDialog();
            if (accepted == true && selector.SelectedScreenRegion is Rect region)
            {
                _selectedRegion = region;
                RegionInfoText.Text = $"Selected: {region.Width:0} × {region.Height:0} px";
                StatusText.Text = "Capture region selected. Press Start preview.";
            }
            else
            {
                StatusText.Text = "Region selection cancelled. Previous selection was kept.";
            }
        }
        finally
        {
            Show();
            Activate();
        }
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        StopPreview();

        int captureIndex = CaptureMethodBox.SelectedIndex;
        int trackingIndex = TrackingModeBox.SelectedIndex;

        if (captureIndex is 1 or 2)
        {
            StatusText.Text = captureIndex == 1
                ? "Windows Graphics Capture is not implemented yet. Choose Automatic or GDI BitBlt."
                : "DXGI Desktop Duplication is not implemented yet. Choose Automatic or GDI BitBlt.";
            return;
        }

        if (trackingIndex is 2 or 3)
        {
            StatusText.Text = trackingIndex == 2
                ? "Text-caret tracking is not implemented yet. Choose Fixed region or Mouse pointer."
                : "Automatic tracking is not implemented yet. Choose Fixed region or Mouse pointer.";
            return;
        }

        _provider = new GdiBitBltCaptureProvider();
        var provider = _provider;
        var availability = await provider.CheckAvailabilityAsync();
        if (!availability.IsAvailable)
        {
            StatusText.Text = availability.Message;
            await provider.DisposeAsync();
            _provider = null;
            return;
        }

        int outputWidth;
        int outputHeight;
        switch (ResolutionBox.SelectedIndex)
        {
            case 0: outputWidth = 320; outputHeight = 240; break;
            case 2: outputWidth = 480; outputHeight = 800; break;
            default: outputWidth = 480; outputHeight = 320; break;
        }

        if (PortraitCheckBox.IsChecked == true && outputWidth > outputHeight)
            (outputWidth, outputHeight) = (outputHeight, outputWidth);

        string fpsText = (FpsBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "10 FPS";
        int targetFps = int.TryParse(fpsText.Split(' ')[0], out int parsedFps) ? parsedFps : 10;
        double zoom = Math.Max(1.0, ZoomSlider.Value);

        bool trackMouse = trackingIndex == 1;
        Rect sourceRegion = trackMouse
            ? GdiBitBltCaptureProvider.GetPrimaryScreenBounds()
            : (_selectedRegion ?? GetDefaultRegion(outputWidth, outputHeight));
        Rect captureRegion = CalculateCrop(sourceRegion, outputWidth, outputHeight, zoom, null);

        _previewCts = new CancellationTokenSource();
        _frameCount = 0;
        _fpsClock.Restart();
        CaptureInfoText.Text = "Capture: GDI BitBlt";
        StatusText.Text = trackMouse
            ? $"Mouse tracking active: {outputWidth} × {outputHeight} at {targetFps} FPS."
            : $"Fixed region active: {outputWidth} × {outputHeight} at {targetFps} FPS.";
        StartButton.IsEnabled = false;
        PreviewPlaceholder.Visibility = Visibility.Collapsed;

        var token = _previewCts.Token;
        _previewTask = Task.Run(() => PreviewLoopAsync(
            provider, sourceRegion, captureRegion, outputWidth, outputHeight, targetFps, zoom, trackMouse, token));
    }

    private static Rect GetDefaultRegion(int outputWidth, int outputHeight)
    {
        Rect screen = GdiBitBltCaptureProvider.GetPrimaryScreenBounds();
        Rect desired = new(
            screen.Left + (screen.Width - outputWidth) / 2,
            screen.Top + (screen.Height - outputHeight) / 2,
            outputWidth, outputHeight);
        return ClampToBounds(desired, screen);
    }

    private static Rect CalculateCrop(
        Rect source, int outputWidth, int outputHeight, double zoom, Point? requestedCenter)
    {
        double outputAspect = (double)outputWidth / outputHeight;
        double sourceWidth = source.Width;
        double sourceHeight = source.Height;

        if (sourceWidth / sourceHeight > outputAspect)
            sourceWidth = sourceHeight * outputAspect;
        else
            sourceHeight = sourceWidth / outputAspect;

        sourceWidth = Math.Max(1, sourceWidth / zoom);
        sourceHeight = Math.Max(1, sourceHeight / zoom);

        Point center = requestedCenter ?? new Point(
            source.Left + source.Width / 2,
            source.Top + source.Height / 2);

        var desired = new Rect(
            center.X - sourceWidth / 2,
            center.Y - sourceHeight / 2,
            sourceWidth, sourceHeight);

        return ClampToBounds(desired, source);
    }

    private static Rect ClampToBounds(Rect rectangle, Rect bounds)
    {
        double width = Math.Min(rectangle.Width, bounds.Width);
        double height = Math.Min(rectangle.Height, bounds.Height);
        double x = Math.Clamp(rectangle.X, bounds.Left, bounds.Right - width);
        double y = Math.Clamp(rectangle.Y, bounds.Top, bounds.Bottom - height);
        return new Rect(x, y, width, height);
    }

    private async Task PreviewLoopAsync(
        GdiBitBltCaptureProvider provider, Rect sourceRegion, Rect fixedCaptureRegion,
        int outputWidth, int outputHeight, int targetFps, double zoom,
        bool trackMouse, CancellationToken token)
    {
        TimeSpan interval = TimeSpan.FromMilliseconds(1000.0 / targetFps);
        try
        {
            while (!token.IsCancellationRequested)
            {
                long started = Stopwatch.GetTimestamp();
                Rect captureRegion = trackMouse
                    ? CalculateCrop(sourceRegion, outputWidth, outputHeight, zoom, GetCursorPosition())
                    : fixedCaptureRegion;
                BitmapSource? frame = await provider.CaptureAsync(captureRegion, token);

                if (frame is not null)
                {
                    BitmapSource output = frame;
                    if (frame.PixelWidth != outputWidth || frame.PixelHeight != outputHeight)
                    {
                        var resized = new TransformedBitmap(frame,
                            new System.Windows.Media.ScaleTransform(
                                (double)outputWidth / frame.PixelWidth,
                                (double)outputHeight / frame.PixelHeight));
                        resized.Freeze();
                        output = resized;
                    }

                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (token.IsCancellationRequested)
                            return;

                        PreviewImage.Source = output;
                        _frameCount++;
                        if (_fpsClock.ElapsedMilliseconds >= 1000)
                        {
                            double actualFps = _frameCount / _fpsClock.Elapsed.TotalSeconds;
                            FpsInfoText.Text = $"FPS: {actualFps:F1}";
                            _frameCount = 0;
                            _fpsClock.Restart();
                        }
                    });
                }

                double elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                int delay = Math.Max(1, (int)(interval.TotalMilliseconds - elapsed));
                await Task.Delay(delay, token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the user stops preview.
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (!token.IsCancellationRequested)
                    StatusText.Text = $"Capture error: {ex.Message}";
            });
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e) => StopPreview();

    private void StopPreview()
    {
        _previewCts?.Cancel();
        _previewCts?.Dispose();
        _previewCts = null;

        if (_provider is not null)
        {
            _ = _provider.DisposeAsync();
            _provider = null;
        }

        PreviewImage.Source = null;
        PreviewPlaceholder.Visibility = Visibility.Visible;
        CaptureInfoText.Text = "Capture: stopped";
        FpsInfoText.Text = "FPS: —";
        StartButton.IsEnabled = true;
    }

    private static Point? GetCursorPosition()
    {
        if (!GetCursorPos(out NativePoint point))
            return null;
        return new Point(point.X, point.Y);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowDisplayAffinity(IntPtr window, uint affinity);
}

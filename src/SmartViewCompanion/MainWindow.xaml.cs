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
    private bool _isClosing;

    private const uint WdaExcludeFromCapture = 0x00000011;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => TryExcludeWindowFromCapture();
        Closing += MainWindow_Closing;
    }

    private void TryExcludeWindowFromCapture()
    {
        try
        {
            var handle = new WindowInteropHelper(this).Handle;
            bool applied = SetWindowDisplayAffinity(handle, WdaExcludeFromCapture);
            CaptureExclusionText.Text = applied
                ? "Main window exclusion: requested (best effort)"
                : "Main window exclusion: unavailable on this Windows configuration";
        }
        catch
        {
            CaptureExclusionText.Text = "Main window exclusion: unavailable";
        }
    }

    private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isClosing)
            return;

        e.Cancel = true;
        _isClosing = true;
        await StopPreviewAsync();
        Close();
    }

    private async void SelectRegionButton_Click(object sender, RoutedEventArgs e)
    {
        await StopPreviewAsync();
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
        await StopPreviewAsync();
        StartButton.IsEnabled = false;

        int captureIndex = CaptureMethodBox.SelectedIndex;
        int trackingIndex = TrackingModeBox.SelectedIndex;

        if (captureIndex is 1 or 2)
        {
            StatusText.Text = captureIndex == 1
                ? "Windows Graphics Capture is not implemented yet. Choose Automatic or GDI BitBlt."
                : "DXGI Desktop Duplication is not implemented yet. Choose Automatic or GDI BitBlt.";
            StartButton.IsEnabled = true;
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
            StartButton.IsEnabled = true;
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
        bool trackCaret = trackingIndex == 2;
        bool trackAutomatic = trackingIndex == 3;
        Rect sourceRegion = trackMouse || trackCaret || trackAutomatic
            ? GdiBitBltCaptureProvider.GetPrimaryScreenBounds()
            : (_selectedRegion ?? GetDefaultRegion(outputWidth, outputHeight));
        Rect captureRegion = CalculateCrop(sourceRegion, outputWidth, outputHeight, zoom, null);

        _previewCts = new CancellationTokenSource();
        _frameCount = 0;
        _fpsClock.Restart();
        CaptureInfoText.Text = "Capture: GDI BitBlt";
        StatusText.Text = trackingIndex switch
        {
            1 => $"Mouse tracking active: {outputWidth} × {outputHeight} at {targetFps} FPS.",
            2 => $"Text-caret tracking active: {outputWidth} × {outputHeight} at {targetFps} FPS. Some apps do not expose a Win32 caret.",
            3 => $"Automatic tracking active (caret first, mouse fallback): {outputWidth} × {outputHeight} at {targetFps} FPS.",
            _ => $"Fixed region active: {outputWidth} × {outputHeight} at {targetFps} FPS."
        };
        StartButton.IsEnabled = false;
        PreviewPlaceholder.Visibility = Visibility.Collapsed;

        var token = _previewCts.Token;
        _previewTask = Task.Run(() => PreviewLoopAsync(
            provider, sourceRegion, captureRegion, outputWidth, outputHeight,
            targetFps, zoom, trackMouse, trackCaret, trackAutomatic, token));
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
        bool trackMouse, bool trackCaret, bool trackAutomatic, CancellationToken token)
    {
        TimeSpan interval = TimeSpan.FromMilliseconds(1000.0 / targetFps);
        Point lastCaretPosition = new(
            sourceRegion.Left + sourceRegion.Width / 2,
            sourceRegion.Top + sourceRegion.Height / 2);
        bool caretWasFound = false;

        try
        {
            while (!token.IsCancellationRequested)
            {
                long started = Stopwatch.GetTimestamp();
                Point? trackingPoint = null;

                if (trackMouse)
                    trackingPoint = GetCursorPosition();
                else if (trackCaret || trackAutomatic)
                {
                    trackingPoint = GetForegroundCaretPosition();
                    if (trackingPoint is Point caretPoint)
                    {
                        lastCaretPosition = caretPoint;
                        caretWasFound = true;
                    }
                    else if (trackAutomatic)
                    {
                        trackingPoint = GetCursorPosition();
                        caretWasFound = false;
                    }
                    else
                        trackingPoint = lastCaretPosition;
                }

                Rect captureRegion = trackMouse || trackCaret || trackAutomatic
                    ? CalculateCrop(sourceRegion, outputWidth, outputHeight, zoom, trackingPoint)
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

                        if (trackCaret && !caretWasFound)
                            StatusText.Text = "Waiting for a supported text caret. Showing the last known/center position; some apps do not expose caret coordinates.";
                        else if (trackAutomatic && !caretWasFound)
                            StatusText.Text = "Automatic mode: no Win32 caret detected; following the mouse pointer.";
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

    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        await StopPreviewAsync();
    }

    /// <summary>
    /// Stops the capture loop before disposing its provider. Awaiting the loop
    /// prevents overlapping sessions and resource disposal while a frame is in flight.
    /// </summary>
    private async Task StopPreviewAsync()
    {
        CancellationTokenSource? cts = _previewCts;
        Task? previewTask = _previewTask;
        IFrameCaptureProvider? provider = _provider;

        _previewCts = null;
        _previewTask = null;
        _provider = null;

        cts?.Cancel();

        if (previewTask is not null)
        {
            try
            {
                await previewTask;
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
            catch (Exception)
            {
                // The preview loop reports runtime errors to the status area.
            }
        }

        if (provider is not null)
            await provider.DisposeAsync();

        cts?.Dispose();

        PreviewImage.Source = null;
        PreviewPlaceholder.Visibility = Visibility.Visible;
        CaptureInfoText.Text = "Capture: stopped";
        FpsInfoText.Text = "FPS: —";

        if (!_isClosing)
            StartButton.IsEnabled = true;
    }

    private static Point? GetCursorPosition()
    {
        if (!GetCursorPos(out NativePoint point))
            return null;
        return new Point(point.X, point.Y);
    }

    /// <summary>
    /// Reads the foreground thread's caret rectangle and converts its top-left
    /// client coordinate to desktop coordinates. This works only for applications
    /// that expose a standard Win32 GUI caret.
    /// </summary>
    private static Point? GetForegroundCaretPosition()
    {
        IntPtr foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
            return null;

        uint threadId = GetWindowThreadProcessId(foregroundWindow, out _);
        if (threadId == 0)
            return null;

        var info = new GuiThreadInfo
        {
            Size = (uint)Marshal.SizeOf<GuiThreadInfo>()
        };

        if (!GetGUIThreadInfo(threadId, ref info) || info.CaretWindow == IntPtr.Zero)
            return null;

        var point = new NativePoint
        {
            X = info.CaretRect.Left,
            Y = info.CaretRect.Top
        };

        if (!ClientToScreen(info.CaretWindow, ref point))
            return null;

        return new Point(point.X, point.Y);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        public uint Size;
        public uint Flags;
        public IntPtr ActiveWindow;
        public IntPtr FocusWindow;
        public IntPtr CaptureWindow;
        public IntPtr MenuOwner;
        public IntPtr MoveSizeWindow;
        public IntPtr CaretWindow;
        public NativeRect CaretRect;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetGUIThreadInfo(uint threadId, ref GuiThreadInfo info);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ClientToScreen(IntPtr window, ref NativePoint point);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowDisplayAffinity(IntPtr window, uint affinity);
}

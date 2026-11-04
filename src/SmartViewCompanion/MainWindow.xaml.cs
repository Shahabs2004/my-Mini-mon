using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SmartViewCompanion.Capture;

namespace SmartViewCompanion;

public partial class MainWindow : Window
{
    private CancellationTokenSource? _previewCts;
    private Task? _previewTask;
    private GdiBitBltCaptureProvider? _provider;
    private long _frameCount;
    private readonly Stopwatch _fpsClock = Stopwatch.StartNew();

    public MainWindow()
    {
        InitializeComponent();
        Closing += (_, _) => StopPreview();
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

        if (trackingIndex != 0)
        {
            StatusText.Text = "This build supports Fixed region only. Mouse and text-caret tracking are planned next.";
            return;
        }

        _provider = new GdiBitBltCaptureProvider();
        var availability = await _provider.CheckAvailabilityAsync();
        if (!availability.IsAvailable)
        {
            StatusText.Text = availability.Message;
            await _provider.DisposeAsync();
            _provider = null;
            return;
        }

        int[] sizes = { 0, 0 };
        switch (ResolutionBox.SelectedIndex)
        {
            case 0: sizes = new[] { 320, 240 }; break;
            case 1: sizes = new[] { 480, 320 }; break;
            case 2: sizes = new[] { 480, 800 }; break;
            default: sizes = new[] { 480, 320 }; break;
        }

        int outputWidth = sizes[0];
        int outputHeight = sizes[1];
        if (PortraitCheckBox.IsChecked == true && outputWidth > outputHeight)
            (outputWidth, outputHeight) = (outputHeight, outputWidth);

        string fpsText = (FpsBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "10 FPS";
        int targetFps = int.TryParse(fpsText.Split(' ')[0], out int parsedFps) ? parsedFps : 10;
        double zoom = Math.Max(1.0, ZoomSlider.Value);

        _previewCts = new CancellationTokenSource();
        _frameCount = 0;
        _fpsClock.Restart();
        CaptureInfoText.Text = "Capture: GDI BitBlt";
        StatusText.Text = $"Running: {outputWidth} × {outputHeight} at {targetFps} FPS. Source: center of primary display.";
        StartButton.IsEnabled = false;

        var token = _previewCts.Token;
        _previewTask = Task.Run(() => PreviewLoopAsync(outputWidth, outputHeight, targetFps, zoom, token));
    }

    private async Task PreviewLoopAsync(int outputWidth, int outputHeight, int targetFps, double zoom, CancellationToken token)
    {
        if (_provider is null)
            return;

        TimeSpan interval = TimeSpan.FromMilliseconds(1000.0 / targetFps);
        Rect screen = GdiBitBltCaptureProvider.GetPrimaryScreenBounds();
        int cropWidth = Math.Max(1, (int)Math.Round(outputWidth / zoom));
        int cropHeight = Math.Max(1, (int)Math.Round(outputHeight / zoom));
        int left = (int)(screen.Width - cropWidth) / 2;
        int top = (int)(screen.Height - cropHeight) / 2;
        var region = new Rect(left, top, cropWidth, cropHeight);

        try
        {
            while (!token.IsCancellationRequested)
            {
                var frameStarted = Stopwatch.GetTimestamp();
                BitmapSource? frame = await _provider.CaptureAsync(region, token);
                if (frame is not null)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        PreviewImage.Source = frame;
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

                double elapsedMs = Stopwatch.GetElapsedTime(frameStarted).TotalMilliseconds;
                int delayMs = Math.Max(1, (int)(interval.TotalMilliseconds - elapsedMs));
                await Task.Delay(delayMs, token);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal stop.
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() => StatusText.Text = $"Capture error: {ex.Message}");
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
        CaptureInfoText.Text = "Capture: stopped";
        FpsInfoText.Text = "FPS: —";
        StartButton.IsEnabled = true;
        StatusText.Text = "Status: Preview stopped.";
    }
}

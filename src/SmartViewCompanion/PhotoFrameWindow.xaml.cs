using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using SmartViewCompanion.Core;

namespace SmartViewCompanion;

public partial class PhotoFrameWindow : Window
{
    private readonly PhotoFrameSettings _settings;
    private IReadOnlyList<string> _images = [];
    private int _currentIndex = -1;
    private DateTime _lastSlideAt = DateTime.MinValue;
    private readonly System.Windows.Threading.DispatcherTimer _timer;

    public PhotoFrameWindow()
    {
        InitializeComponent();
        _settings = PhotoFrameSettingsStore.Load();
        ApplySettings();
        RefreshLibrary();

        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();
        Closed += (_, _) => _timer.Stop();
    }

    private void ApplySettings()
    {
        SelectByTag(IdleMinutesBox, _settings.IdleMinutes.ToString(), "2");
        SelectByTag(SlideSecondsBox, _settings.SlideSeconds.ToString(), "10");
        SelectByTag(FitModeBox, _settings.FitMode, "Uniform");
        PhotoImage.Stretch = _settings.FitMode == "UniformToFill" ? Stretch.UniformToFill : Stretch.Uniform;
    }

    private static void SelectByTag(ComboBox combo, string value, string fallback)
    {
        combo.SelectedItem = combo.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), value, StringComparison.Ordinal))
            ?? combo.Items.Cast<ComboBoxItem>().First(item => item.Tag?.ToString() == fallback);
    }

    private void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select photos",
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff;*.wdp|All files|*.*",
            Multiselect = true,
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            _settings.Sources.AddRange(dialog.FileNames);
            _settings.Sources = _settings.Sources.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            RefreshLibrary();
            SaveSettings();
        }
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        // OpenFileDialog's folder-selection pattern avoids adding a WinForms dependency.
        var dialog = new OpenFileDialog
        {
            Title = "Select a folder containing photos",
            CheckFileExists = false,
            CheckPathExists = true,
            FileName = "Select this folder",
            ValidateNames = false,
            Filter = "Folder selection|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            string? folder = Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
            {
                _settings.Sources.Add(folder);
                _settings.Sources = _settings.Sources.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                RefreshLibrary();
                SaveSettings();
            }
        }
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        StatusText.Text = "Photo-frame settings saved.";
    }

    private void SaveSettings()
    {
        _settings.IdleMinutes = GetSelectedInt(IdleMinutesBox, 2);
        _settings.SlideSeconds = GetSelectedInt(SlideSecondsBox, 10);
        _settings.FitMode = (FitModeBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Uniform";
        PhotoFrameSettingsStore.Save(_settings);
    }

    private static int GetSelectedInt(ComboBox combo, int fallback)
    {
        return int.TryParse((combo.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out int value)
            ? value : fallback;
    }

    private void RefreshLibrary()
    {
        _images = PhotoFrameLibrary.GetImages(_settings.Sources);
        _currentIndex = -1;
        CountText.Text = $"{_images.Count} images";
        SourcesText.Text = $"{_settings.Sources.Count} source(s): " +
            string.Join(", ", _settings.Sources.Select(Path.GetFileName));
        if (_images.Count == 0)
        {
            PhotoImage.Source = null;
            OverlayText.Text = "No supported images found. Add one or more image files or folders.";
            OverlayText.Visibility = Visibility.Visible;
            StatusText.Text = "Photo library is empty.";
        }
        else
        {
            OverlayText.Visibility = Visibility.Visible;
            OverlayText.Text = "Waiting for user inactivity…";
            StatusText.Text = $"Loaded {_images.Count} image(s). Slideshow starts after the selected idle period.";
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (_images.Count == 0)
            return;

        int idleMinutes = GetSelectedInt(IdleMinutesBox, _settings.IdleMinutes);
        TimeSpan idleFor = UserIdleDetector.GetIdleTime();
        if (idleFor < TimeSpan.FromMinutes(idleMinutes))
        {
            OverlayText.Visibility = Visibility.Visible;
            OverlayText.Text = $"Waiting for inactivity… ({Math.Max(0, idleMinutes * 60 - idleFor.TotalSeconds):0} sec)";
            StatusText.Text = "User activity detected; photo frame is paused.";
            return;
        }

        OverlayText.Visibility = Visibility.Collapsed;
        int slideSeconds = GetSelectedInt(SlideSecondsBox, _settings.SlideSeconds);
        if (_currentIndex < 0 || DateTime.UtcNow - _lastSlideAt >= TimeSpan.FromSeconds(slideSeconds))
            ShowNextImage();
        StatusText.Text = "Idle detected — photo frame is active.";
    }

    private void ShowNextImage()
    {
        if (_images.Count == 0)
            return;

        for (int attempt = 0; attempt < _images.Count; attempt++)
        {
            _currentIndex = (_currentIndex + 1) % _images.Count;
            try
            {
                PhotoImage.Source = PhotoFrameLibrary.Load(_images[_currentIndex]);
                PhotoImage.Stretch = (FitModeBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() == "UniformToFill"
                    ? Stretch.UniformToFill : Stretch.Uniform;
                _lastSlideAt = DateTime.UtcNow;
                CountText.Text = $"{_currentIndex + 1}/{_images.Count} · {Path.GetFileName(_images[_currentIndex])}";
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                StatusText.Text = $"Skipped unreadable image: {Path.GetFileName(_images[_currentIndex])}";
            }
        }

        PhotoImage.Source = null;
        OverlayText.Visibility = Visibility.Visible;
        OverlayText.Text = "No readable images remain. Check the selected files and folders.";
    }
}

internal static class UserIdleDetector
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LastInputInfo plii);

    public static TimeSpan GetIdleTime()
    {
        var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        if (!GetLastInputInfo(ref info))
            return TimeSpan.Zero;

        uint elapsedMilliseconds = unchecked((uint)Environment.TickCount - info.Time);
        return TimeSpan.FromMilliseconds(elapsedMilliseconds);
    }
}

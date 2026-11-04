namespace SmartViewCompanion.Core;

/// <summary>How desktop pixels are acquired. Independent from tracking mode.</summary>
public enum CaptureMethod
{
    Automatic,
    WindowsGraphicsCapture,
    DxgiDesktopDuplication,
    GdiBitBlt
}

/// <summary>Which screen location determines the crop position.</summary>
public enum TrackingMode
{
    FixedRegion,
    Mouse,
    Caret,
    Automatic
}

/// <summary>What desktop surface is the source of pixels.</summary>
public enum CaptureSourceKind
{
    PrimaryMonitor,
    SelectedMonitor,
    Window,
    Region
}

public sealed class CaptureOptions
{
    public CaptureMethod Method { get; set; } = CaptureMethod.Automatic;
    public TrackingMode Tracking { get; set; } = TrackingMode.FixedRegion;
    public CaptureSourceKind Source { get; set; } = CaptureSourceKind.PrimaryMonitor;
    public int OutputWidth { get; set; } = 480;
    public int OutputHeight { get; set; } = 320;
    public int TargetFps { get; set; } = 15;
    public double Zoom { get; set; } = 1.0;
    public bool Portrait { get; set; }
}

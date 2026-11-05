namespace SmartViewCompanion.Core;

public sealed class PhotoFrameSettings
{
    public List<string> Sources { get; set; } = [];
    public int IdleMinutes { get; set; } = 2;
    public int SlideSeconds { get; set; } = 10;
    public string FitMode { get; set; } = "Uniform";
}

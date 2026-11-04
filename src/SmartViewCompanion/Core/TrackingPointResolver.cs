using System.Windows;

namespace SmartViewCompanion.Core;

public readonly record struct TrackingPointResult(Point? Position, bool CaretFound);

/// <summary>Resolves the tracking anchor independently from capture and UI code.</summary>
public static class TrackingPointResolver
{
    public static TrackingPointResult Resolve(
        TrackingMode mode, Point? caretPosition, Point? mousePosition, Point lastKnownCaretPosition)
    {
        return mode switch
        {
            TrackingMode.FixedRegion => new TrackingPointResult(null, false),
            TrackingMode.Mouse => new TrackingPointResult(mousePosition, false),
            TrackingMode.Caret => new TrackingPointResult(caretPosition ?? lastKnownCaretPosition, caretPosition.HasValue),
            TrackingMode.Automatic => new TrackingPointResult(caretPosition ?? mousePosition, caretPosition.HasValue),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }
}

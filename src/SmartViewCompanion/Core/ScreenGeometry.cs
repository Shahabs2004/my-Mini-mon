using System.Windows;

namespace SmartViewCompanion.Core;

public static class ScreenGeometry
{
    public static Rect ClampToBounds(Rect rectangle, Rect bounds)
    {
        if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(bounds));
        if (rectangle.IsEmpty || rectangle.Width <= 0 || rectangle.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(rectangle));
        double width = Math.Min(rectangle.Width, bounds.Width);
        double height = Math.Min(rectangle.Height, bounds.Height);
        double x = Math.Clamp(rectangle.X, bounds.Left, bounds.Right - width);
        double y = Math.Clamp(rectangle.Y, bounds.Top, bounds.Bottom - height);
        return new Rect(x, y, width, height);
    }

    public static Rect GetBoundingBounds(IEnumerable<Rect> rectangles)
    {
        ArgumentNullException.ThrowIfNull(rectangles);
        var items = rectangles.ToArray();
        if (items.Length == 0)
            throw new ArgumentException("At least one rectangle is required.", nameof(rectangles));
        double left = items.Min(r => r.Left);
        double top = items.Min(r => r.Top);
        double right = items.Max(r => r.Right);
        double bottom = items.Max(r => r.Bottom);
        return new Rect(left, top, right - left, bottom - top);
    }

    public static Rect GetCenteredRegion(Rect bounds, double width, double height)
    {
        var desired = new Rect(bounds.Left + (bounds.Width - width) / 2,
            bounds.Top + (bounds.Height - height) / 2, width, height);
        return ClampToBounds(desired, bounds);
    }

    /// <summary>Returns an output-sized crop with one desktop pixel per output pixel.</summary>
    public static Rect CalculateCrop(Rect source, int outputWidth, int outputHeight, Point? requestedCenter)
    {
        if (source.IsEmpty || source.Width <= 0 || source.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(source));
        if (outputWidth <= 0 || outputHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(outputWidth), "Output dimensions must be positive.");
        double width = Math.Min(outputWidth, source.Width);
        double height = Math.Min(outputHeight, source.Height);
        Point center = requestedCenter ?? new Point(source.Left + source.Width / 2, source.Top + source.Height / 2);
        return ClampToBounds(new Rect(center.X - width / 2, center.Y - height / 2, width, height), source);
    }
}
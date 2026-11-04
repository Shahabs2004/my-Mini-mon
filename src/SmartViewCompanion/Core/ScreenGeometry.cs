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

    public static Rect CalculateCrop(Rect source, int outputWidth, int outputHeight, double zoom, Point? requestedCenter)
    {
        if (source.IsEmpty || source.Width <= 0 || source.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(source));
        if (outputWidth <= 0 || outputHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(outputWidth), "Output dimensions must be positive.");
        if (!double.IsFinite(zoom) || zoom <= 0)
            throw new ArgumentOutOfRangeException(nameof(zoom), "Zoom must be finite and positive.");

        double aspect = (double)outputWidth / outputHeight;
        double width = source.Width;
        double height = source.Height;
        if (width / height > aspect) width = height * aspect;
        else height = width / aspect;
        width = Math.Max(1, width / zoom);
        height = Math.Max(1, height / zoom);
        Point center = requestedCenter ?? new Point(source.Left + source.Width / 2, source.Top + source.Height / 2);
        return ClampToBounds(new Rect(center.X - width / 2, center.Y - height / 2, width, height), source);
    }
}

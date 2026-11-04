using System.Windows;
using SmartViewCompanion.Core;
using Xunit;

namespace SmartViewCompanion.Tests;

public sealed class ScreenGeometryTests
{
    [Fact]
    public void GetBoundingBounds_IncludesNegativeMonitorOrigins()
    {
        var monitors = new[]
        {
            new Rect(0, 0, 1920, 1080),
            new Rect(-1600, -200, 1600, 900)
        };

        Rect result = ScreenGeometry.GetBoundingBounds(monitors);

        Assert.Equal(new Rect(-1600, -200, 3520, 1280), result);
    }

    [Fact]
    public void ClampToBounds_ClampsCropInsideNegativeOriginMonitor()
    {
        var bounds = new Rect(-1600, -200, 1600, 900);
        var requested = new Rect(-1700, -300, 500, 400);

        Rect result = ScreenGeometry.ClampToBounds(requested, bounds);

        Assert.Equal(new Rect(-1600, -200, 500, 400), result);
    }

    [Fact]
    public void CalculateCrop_ClampsTrackingPointToVirtualDesktopBounds()
    {
        var virtualDesktop = new Rect(-1600, -200, 3520, 1280);
        var requestedCenter = new Point(1900, 900);

        Rect result = ScreenGeometry.CalculateCrop(
            virtualDesktop, 480, 320, 2.0, requestedCenter);

        Assert.True(virtualDesktop.Contains(result.TopLeft));
        Assert.True(virtualDesktop.Contains(result.BottomRight));
        Assert.True(result.Width > 0);
        Assert.True(result.Height > 0);
    }

    [Fact]
    public void GetCenteredRegion_UsesBoundsOrigin()
    {
        var bounds = new Rect(-1600, -200, 1600, 900);

        Rect result = ScreenGeometry.GetCenteredRegion(bounds, 480, 320);

        Assert.Equal(new Rect(-1040, 90, 480, 320), result);
    }
}

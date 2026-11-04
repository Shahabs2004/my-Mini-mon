using System.Windows;
using SmartViewCompanion.Core;
using Xunit;

namespace SmartViewCompanion.Tests;

public sealed class TrackingPointResolverTests
{
    private static readonly Point LastCaret = new(120, 240);
    private static readonly Point Mouse = new(800, 600);
    private static readonly Point Caret = new(300, 400);

    [Fact]
    public void Automatic_PrefersCaretWhenAvailable()
    {
        var result = TrackingPointResolver.Resolve(TrackingMode.Automatic, Caret, Mouse, LastCaret);

        Assert.Equal(Caret, result.Position);
        Assert.True(result.CaretFound);
    }

    [Fact]
    public void Automatic_FallsBackToMouseWhenCaretIsUnavailable()
    {
        var result = TrackingPointResolver.Resolve(TrackingMode.Automatic, null, Mouse, LastCaret);

        Assert.Equal(Mouse, result.Position);
        Assert.False(result.CaretFound);
    }

    [Fact]
    public void CaretMode_UsesLastKnownPositionWhenCaretIsUnavailable()
    {
        var result = TrackingPointResolver.Resolve(TrackingMode.Caret, null, Mouse, LastCaret);

        Assert.Equal(LastCaret, result.Position);
        Assert.False(result.CaretFound);
    }

    [Fact]
    public void MouseMode_IgnoresCaret()
    {
        var result = TrackingPointResolver.Resolve(TrackingMode.Mouse, Caret, Mouse, LastCaret);

        Assert.Equal(Mouse, result.Position);
        Assert.False(result.CaretFound);
    }

    [Fact]
    public void FixedMode_DoesNotReturnTrackingPoint()
    {
        var result = TrackingPointResolver.Resolve(TrackingMode.FixedRegion, Caret, Mouse, LastCaret);

        Assert.Null(result.Position);
        Assert.False(result.CaretFound);
    }
}

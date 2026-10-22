using FrontlineMap.Core;
using Xunit;

public class ContourProjectionMetricTests
{
    // Replacing the metric comparison with transform equality recreates rotation rebuilds.
    [Fact] public void RigidScreenRotationPreservesLocalStrokeOffsets()
    {
        Assert.True(ContourProjectionMetric.TryCreate(2, 0, 0, 3, 0, 0, 1, out var original));
        for (int degrees = 0; degrees < 360; degrees++)
        {
            double a = degrees * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a);
            Assert.True(ContourProjectionMetric.TryCreate(2*c, -3*s, 2*s, 3*c, 0, 0, 1, out var rotated));
            Assert.True(original.SameWidths(rotated));
        }
    }
    [Theory]
    [InlineData(2.1, 0, 0, 3)] // Zoom in one axis.
    [InlineData(2, 1, 0, 3)] // Shear changes the width of oblique segments.
    [InlineData(3, 0, 0, 2)] // Local rotation inside a nonuniform parent scale.
    [InlineData(-2, 0, 0, 3)] // Reflections conservatively rebuild winding.
    public void ChangedProjectedWidthsInvalidate(double xx, double xy, double yx, double yy)
    {
        Assert.True(ContourProjectionMetric.TryCreate(2, 0, 0, 3, 0, 0, 1, out var original));
        Assert.True(ContourProjectionMetric.TryCreate(xx, xy, yx, yy, 0, 0, 1, out var changed));
        Assert.False(original.SameWidths(changed));
    }
    [Fact] public void ConstantPerspectiveDepthIncludesDistanceInPixelScale()
    {
        Assert.True(ContourProjectionMetric.TryCreate(4, 0, 0, 6, 0, 0, 2, out var near));
        Assert.True(ContourProjectionMetric.TryCreate(4, 0, 0, 6, 0, 0, 4, out var far));
        Assert.False(near.SameWidths(far));
        Assert.True(ContourProjectionMetric.TryCreate(2, 0, 0, 3, 0, 0, 1, out var affine));
        Assert.True(near.SameWidths(affine));
    }
    [Theory]
    [InlineData(.001, 0, 1)]
    [InlineData(0, .001, 1)]
    [InlineData(0, 0, 0)]
    [InlineData(0, 0, double.NaN)]
    public void PerspectiveAndInvalidDepthRequireConservativeFallback(double wx, double wy, double w)
    {
        Assert.False(ContourProjectionMetric.TryCreate(2, 0, 0, 3, wx, wy, w, out _));
    }
    [Theory]
    [InlineData(0, 0, 0, 3)]
    [InlineData(2, 4, 1, 2)]
    [InlineData(double.PositiveInfinity, 0, 0, 3)]
    public void DegenerateOrNonfiniteProjectionRequiresConservativeFallback(double xx, double xy, double yx, double yy)
    {
        Assert.False(ContourProjectionMetric.TryCreate(xx, xy, yx, yy, 0, 0, 1, out _));
    }
    [Fact] public void FloatRotationNoiseIsToleratedButAccumulatedZoomIsNot()
    {
        Assert.True(ContourProjectionMetric.TryCreate(2, 0, 0, 3, 0, 0, 1, out var original));
        Assert.True(ContourProjectionMetric.TryCreate(2.0000001, 0, 0, 3, 0, 0, 1, out var noisy));
        Assert.True(original.SameWidths(noisy));
        Assert.True(ContourProjectionMetric.TryCreate(2.0002, 0, 0, 3, 0, 0, 1, out var zoomed));
        Assert.False(original.SameWidths(zoomed));
    }
    [Fact] public void LargeScaleOnOneAxisDoesNotHideWidthChangesOnOtherAxis()
    {
        Assert.True(ContourProjectionMetric.TryCreate(10000, 0, 0, 1, 0, 0, 1, out var original));
        Assert.True(ContourProjectionMetric.TryCreate(10000, 0, 0, 1.01, 0, 0, 1, out var changed));
        Assert.False(original.SameWidths(changed));
    }
}

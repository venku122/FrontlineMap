using FrontlineMap.Core;
using Xunit;

public class ContourProjectionMetricTests
{
    // A wrong inverse, determinant sign, or normalization changes the projected
    // stroke width or slants its cross-section relative to the segment.
    [Theory]
    [InlineData(1, 0, 0, 1)]
    [InlineData(0, -1, 1, 0)]
    [InlineData(2, 0, 0, 3)]
    [InlineData(2, 1, .5, 3)]
    [InlineData(-2, 1, .5, 3)]
    [InlineData(.6, -.8, .8, .6)]
    [InlineData(10000, 0, 0, 1)]
    public void AffineOffsetProjectsToThreePixelPerpendicularStroke(double xx, double xy, double yx, double yy)
    {
        Assert.True(ContourProjectionMetric.TryCreate(xx, xy, yx, yy, 0, 0, 1, out var metric));
        foreach (var direction in new[] { (1d, 0d), (0d, 1d), (3d, 4d), (-2d, 5d) })
        {
            Assert.True(metric.TryGetHalfWidthOffset(direction.Item1, direction.Item2, 3, out double nx, out double ny));
            double sx = xx*nx + xy*ny, sy = yx*nx + yy*ny;
            double dx = xx*direction.Item1 + xy*direction.Item2, dy = yx*direction.Item1 + yy*direction.Item2;
            Assert.Equal(3, 2*Math.Sqrt(sx*sx + sy*sy), 8);
            Assert.InRange(Math.Abs((sx*dx + sy*dy)/Math.Sqrt(dx*dx + dy*dy)), 0, 1e-8);
            Assert.True(dx*sy-dy*sx > 0); // Same screen-side ordering as the old path.
        }
    }
    [Theory]
    [InlineData(0, 0, 3)]
    [InlineData(double.NaN, 1, 3)]
    [InlineData(1, 1, double.NaN)]
    [InlineData(1, 1, -1)]
    public void InvalidAffineStrokeCannotEmitVertices(double dx, double dy, double width)
    {
        Assert.True(ContourProjectionMetric.TryCreate(1, 0, 0, 1, 0, 0, 1, out var metric));
        Assert.False(metric.TryGetHalfWidthOffset(dx, dy, width, out _, out _));
        Assert.False(default(ContourProjectionMetric).TryGetHalfWidthOffset(1, 0, 3, out _, out _));
    }
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

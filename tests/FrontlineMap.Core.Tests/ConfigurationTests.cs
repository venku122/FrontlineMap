using Xunit;
using FrontlineMap.Core;
public class ConfigurationTests
{
    [Theory][InlineData(double.NaN)][InlineData(double.PositiveInfinity)][InlineData(0)][InlineData(-1)] public void RejectsInvalidRadius(double r) => Assert.Throws<ArgumentOutOfRangeException>(() => new ModelSettings(64, r, 5, .16, .01, 1e-9, 2, 15));
    [Theory][InlineData(-1)][InlineData(0)][InlineData(4096)] public void RejectsUnsafeResolution(int n) => Assert.Throws<ArgumentOutOfRangeException>(() => ScalarFieldTests.Settings(n));
    [Fact] public void RejectsInvalidOpacityAndAge() => Assert.Throws<ArgumentOutOfRangeException>(() => new ModelSettings(64, 12, 5, double.NaN, .01, 1e-9, 2, 15));
    [Fact] public void InvalidWeightCannotEnterSnapshot() => Assert.Throws<ArgumentOutOfRangeException>(() => ScalarFieldTests.Stamp(weight: double.NaN));
    [Fact] public void InvalidBoundsCannotBind() => Assert.Throws<ArgumentOutOfRangeException>(() => new MapGeometry(0, 0, 0, 8, 8, 8));
    [Fact]
    public void ProjectionHandlesOriginAndCrop()
    {
        var g = new MapGeometry(100, 200, 80, 40, 8, 8);
        Assert.Equal(.5, g.U(140), 9);
        Assert.Equal(.5, g.V(220), 9);
        Assert.Equal(.3, MapGeometry.CroppedCoordinate(.4, .25, .5), 9);
    }
}

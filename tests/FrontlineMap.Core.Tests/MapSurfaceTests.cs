using FrontlineMap.Core;
using Xunit;
public class MapSurfaceTests
{
    [Theory]
    [InlineData(true, "UI/Default", 1, 1, 0, 0, true)]
    [InlineData(false, "UI/AdditiveMasked", 1, 1, 0, 0, true)]
    [InlineData(false, "Custom/DistortedMap", 1, 1, 0, 0, false)]
    [InlineData(false, "UI/AdditiveMasked", 2, 1, 0, 0, false)]
    [InlineData(false, "UI/AdditiveMasked", 1, 1, .25, 0, false)]
    [InlineData(false, "UI/AdditiveMasked", double.NaN, 1, 0, 0, false)]
    [InlineData(false, null, 1, 1, 0, 0, false)]
    public void MaterialMustPreserveTheObservedFullRectProjection(bool standard, string shader, double sx, double sy, double ox, double oy, bool expected)
    {
        Assert.Equal(expected, MapSurfaceContract.SupportsMaterial(standard, shader, sx, sy, ox, oy));
    }
}

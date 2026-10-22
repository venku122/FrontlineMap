using System;
using System.Linq;
using FrontlineMap.Core;
using Xunit;

public class TerrainSourceRegressionTests
{
    static TerrainCostGrid Flat(int n) => new TerrainCostGrid(new MapGeometry(0, 0, 64000, 64000, n, n),
        Enumerable.Repeat(1d, n * n).ToArray(), Enumerable.Repeat(true, n * n).ToArray());
    static InfluenceBuffers Field(TerrainCostGrid terrain, double x = 62.5, double z = 62.5, double radius = 2000, double weight = .6)
    {
        var g = new MapGeometry(0, 0, 64000, 64000, 512, 512);
        var stamp = new ObservationStamp("building", Side.Friendly, x, z, weight, 0, false, ObservationRole.Building);
        var snapshot = new ObservationSnapshot(new SnapshotIdentity(1, 1, 1, 1, 1), g, new[] { stamp }, 0, 0, 15, terrain);
        var settings = new ModelSettings(512, radius, 5, .4, .01, 1e-9, 2, 15, 3,
            new TacticalSettings(TacticalLayer.GroundControl, true, terrain.Geometry.Columns, buildingRadius: radius));
        return TacticalField.Compute(snapshot, settings);
    }

    [Fact] public void ReviewFixtureWeightFourHasPointSixSupportAtExactSource()
    {
        Assert.Equal(.6, Field(Flat(32), weight: 4).Friendly[0], 12);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(2000)]
    public void CoarseTerrainPreservesFullWeightAtSourceEvenWhenCenterExceedsRadius(double radius)
    {
        Assert.Equal(.09, Field(Flat(32), radius: radius).Friendly[0], 12);
    }

    [Fact] public void DirectWithinCellFalloffUsesSourceToPixelDistance()
    {
        var field = Field(Flat(32));
        Assert.Equal(.0675, field.Friendly[8], 12); // 1000 m from source with 2000 m radius.
    }

    [Theory]
    [InlineData(62.5, 62.5, 0)]
    [InlineData(1937.5, 1937.5, 7695)]
    [InlineData(2062.5, 62.5, 16)]
    public void OffCenterSourcesRetainFullWeight(double x, double z, int index)
    {
        Assert.Equal(.09, Field(Flat(32), x, z).Friendly[index], 12);
    }

    [Fact] public void FlatSupportRemainsStableAsTerrainResolutionIncreases()
    {
        var coarse = Field(Flat(32), radius: 6000);
        foreach (int n in new[] { 64, 128, 256 })
        {
            var refined = Field(Flat(n), radius: 6000);
            foreach (int index in new[] { 0, 8, 16, 24, 32, 8 * 512 + 8 })
            {
                Assert.True(coarse.Friendly[index] > 0);
                // The eight-neighbor graph remains an upper-bound distance approximation.
                Assert.InRange(Math.Abs(coarse.Friendly[index] - refined.Friendly[index]), 0, .015);
            }
        }
    }

    [Theory]
    [InlineData(0)] // water
    [InlineData(1)] // unknown
    [InlineData(2)] // steep slope
    public void OffCenterSourceCannotBypassBarrier(int barrier)
    {
        const int n = 32;
        var heights = Enumerable.Repeat(1d, n * n).ToArray();
        var known = Enumerable.Repeat(true, n * n).ToArray();
        for (int row = 0; row < n; row++)
        {
            int i = row * n + 1;
            if (barrier == 0) heights[i] = -1;
            if (barrier == 1) known[i] = false;
            if (barrier == 2) heights[i] = 10000;
        }
        var terrain = new TerrainCostGrid(Flat(n).Geometry, heights, known);
        var field = Field(terrain, x: 1937.5, radius: 6000);
        Assert.True(field.Friendly[15] > 0);
        Assert.Equal(0, field.Friendly[16]);
        Assert.Equal(0, field.Friendly[32]);
    }

    [Fact] public void OffCenterSourceCannotCutDiagonalBlockedCorner()
    {
        var terrain = Flat(32);
        var heights = terrain.Heights.ToArray(); heights[1] = heights[32] = -1;
        terrain = new TerrainCostGrid(terrain.Geometry, heights, terrain.Known.ToArray());
        var field = Field(terrain, 1937.5, 1937.5, 2000);
        Assert.Equal(0, field.Friendly[16 * 512 + 16]);
    }
}

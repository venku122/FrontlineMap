using System;
using System.Linq;
using FrontlineMap.Core;
using Xunit;

public class TerrainTests
{
    static TerrainCostGrid Grid(int columns, int rows, double[] heights, bool[] known = null, double width = 0, double height = 0) =>
        new TerrainCostGrid(new MapGeometry(-10, 20, width == 0 ? columns : width, height == 0 ? rows : height, columns, rows), heights, known ?? Enumerable.Repeat(true, heights.Length).ToArray());
    static double[] Distances(TerrainCostGrid grid, double x = -9.5, double z = 20.5, bool water = false, double radius = 100)
    {
        var output = Enumerable.Repeat(-1d, grid.Geometry.Count).ToArray();
        foreach (int chunk in grid.ComputeDistances(x, z, radius, water, output)) Assert.InRange(chunk, 1, 128);
        return output;
    }
    [Fact] public void DistancesUseHorizontalEightNeighborStepsAndActualSourceOffset()
    {
        var grid = Grid(3, 2, Enumerable.Repeat(1d, 6).ToArray());
        var d = Distances(grid, -9.25, 20.5);
        Assert.Equal(.25, d[0], 10);
        Assert.Equal(1.25, d[1], 10);
        Assert.Equal(.25 + Math.Sqrt(2), d[4], 10);
        Assert.True(double.IsPositiveInfinity(Distances(grid, radius: .9)[1]));
    }
    [Fact] public void WaterBarrierSeparatesLandAndLandIslandSeparatesWater()
    {
        var grid = Grid(3, 2, new double[] { 1, -1, 1, 1, -1, 1 });
        Assert.True(double.IsPositiveInfinity(Distances(grid)[2]));
        Assert.Equal(1, Distances(grid, -8.5, 20.5, true)[4], 10);
        var island = Grid(3, 2, new double[] { -1, 1, -1, -1, 1, -1 });
        Assert.True(double.IsPositiveInfinity(Distances(island, water: true)[2]));
        Assert.All(Distances(grid, water: true), d => Assert.True(double.IsPositiveInfinity(d)));
    }
    [Fact] public void SteepSlopeAndUnknownCellsBlockMovement()
    {
        var slope = Grid(3, 2, new double[] { 1, 10, 1, 1, 10, 1 });
        Assert.True(double.IsPositiveInfinity(Distances(slope)[2]));
        var unknown = Grid(3, 2, Enumerable.Repeat(1d, 6).ToArray(), new[] { true, false, true, true, false, true });
        Assert.True(double.IsPositiveInfinity(Distances(unknown)[2]));
        Assert.False(unknown.Land[1]);
        Assert.False(unknown.Water[1]);
    }
    [Fact] public void SeabedSlopeDoesNotBlockSurfaceNavigation()
    {
        var grid = Grid(3, 2, new double[] { -1, -10, -1, -1, -10, -1 });
        Assert.Equal(2, Distances(grid, water: true)[2], 10);
    }
    [Fact] public void DiagonalCannotCutBlockedCorners()
    {
        var grid = Grid(2, 2, new double[] { 1, -1, -1, 1 });
        Assert.True(double.IsPositiveInfinity(Distances(grid)[3]));
    }
    [Fact] public void GapAllowsLongerPathAroundBarrier()
    {
        var grid = Grid(3, 3, new double[] { 1, -1, 1, 1, -1, 1, 1, 1, 1 });
        Assert.Equal(6, Distances(grid)[2], 10);
    }
    [Fact] public void RectangularGridUsesCorrectCellSizesAndOutsideHasNoSources()
    {
        var grid = Grid(3, 2, Enumerable.Repeat(1d, 6).ToArray(), width: 6, height: 8);
        var d = Distances(grid, -9, 22);
        Assert.Equal(2, d[1], 10);
        Assert.Equal(4, d[3], 10);
        Assert.Equal(Math.Sqrt(20), d[4], 10);
        Assert.Equal(5, grid.CellIndex(-4.01, 27.99));
        Assert.Equal(-1, grid.CellIndex(-4, 22));
        Assert.Equal(-1, grid.CellIndex(double.NaN, 22));
        Assert.All(Distances(grid, -10.01, 22), value => Assert.True(double.IsPositiveInfinity(value)));
    }
    [Fact] public void InputsAreCopiedAndNonfiniteHeightsStayUnknown()
    {
        var heights = new double[] { 1, double.NaN, 1, 1 };
        var known = new[] { true, true, true, true };
        var grid = Grid(2, 2, heights, known);
        heights[0] = -1; known[0] = false;
        Assert.Equal(0, Distances(grid)[0]);
        Assert.True(grid.Land[0]);
        Assert.False(grid.Known[1]);
        Assert.False(grid.Land[1]);
        Assert.False(grid.Water[1]);
    }
    [Fact] public void InvalidRadiusAndUnknownOriginResetAllDistances()
    {
        var grid = Grid(2, 2, new double[] { double.NaN, 1, 1, 1 });
        Assert.All(Distances(grid), d => Assert.True(double.IsPositiveInfinity(d)));
        Assert.All(Distances(grid, -8.5, 20.5, radius: double.NaN), d => Assert.True(double.IsPositiveInfinity(d)));
    }
    [Fact] public void LargeFieldResetsIncrementallyAndBoundsWorkChunks()
    {
        var grid = Grid(32, 32, Enumerable.Repeat(1d, 1024).ToArray());
        var output = new double[1024];
        var iterator = grid.ComputeDistances(-9.5, 20.5, 100, false, output).GetEnumerator();
        Assert.True(iterator.MoveNext());
        Assert.InRange(iterator.Current, 1, 128);
        Assert.Equal(0, output[1023]); // Initial reset cannot monopolize the entire field.
        while (iterator.MoveNext()) Assert.InRange(iterator.Current, 1, 128);
        Assert.Equal(31 * Math.Sqrt(2), output[1023], 9);
    }
}

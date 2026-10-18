using Xunit;
using FrontlineMap.Core;

public class ScalarFieldTests
{
    internal static ModelSettings Settings(int n = 8, double radius = 3) => new ModelSettings(n, radius, 5, .16, .01, 1e-9, 2, 15);
    internal static ObservationSnapshot Snapshot(params ObservationStamp[] stamps) => new ObservationSnapshot(new SnapshotIdentity(1, 1, 1, 1, 1), new MapGeometry(-4, -4, 8, 8, 8, 8), stamps, 0, 0, 15);
    internal static ObservationStamp Stamp(string id = "a", Side side = Side.Friendly, double x = .5, double z = .5, double weight = 10) => new ObservationStamp(id, side, x, z, weight, 0, false);
    [Fact]
    public void EmptyHasNoSupport()
    {
        var f = ScalarField.Compute(Snapshot(), Settings());
        Assert.All(f.Friendly, v => Assert.Equal(0, v));
        Assert.All(f.Enemy, v => Assert.Equal(0, v));
    }
    [Fact]
    public void SingleStampUsesCellCentersAndLegacyKernel()
    {
        var f = ScalarField.Compute(Snapshot(Stamp()), Settings());
        Assert.Equal(10, f.Friendly[4 * 8 + 4], 9);
        Assert.Equal(10 * (1 - 1.0 / 9), f.Friendly[4 * 8 + 5], 9);
        Assert.Equal(0, f.Friendly[0]);
    }
    [Fact]
    public void EqualOpponentsRetainSupport()
    {
        var f = ScalarField.Compute(Snapshot(Stamp(), Stamp("b", Side.Enemy)), Settings());
        Assert.Equal(10, f.Friendly[36]);
        Assert.Equal(10, f.Enemy[36]);
        Assert.Equal(CellClass.Balanced, FieldClassifier.Classify(10, 10, Settings()));
        Assert.Equal(CellClass.Unsupported, FieldClassifier.Classify(0, 0, Settings()));
    }
    [Theory]
    [InlineData(Side.Friendly)]
    [InlineData(Side.Enemy)]
    public void OneSideNeverProducesOpposingFront(Side side)
    {
        var f = ScalarField.Compute(Snapshot(Stamp(side: side)), Settings());
        var p = Rasterizer.Render(f, Settings(), 800, 800);
        Assert.DoesNotContain(p, v => v.Kind == PixelKind.Front);
        Assert.DoesNotContain(p, v => v.Kind == (side == Side.Friendly ? PixelKind.Enemy : PixelKind.Friendly));
        Assert.Contains(p, v => v.A > 0);
    }
    [Fact]
    public void FlatBalancedPlateauIsContestedNotArbitraryLine()
    {
        var f = new InfluenceBuffers(new MapGeometry(-4, -4, 8, 8, 8, 8));
        Array.Fill(f.Friendly, 10);
        Array.Fill(f.Enemy, 10);
        var p = Rasterizer.Render(f, Settings(), 800, 800);
        Assert.All(p, v => Assert.Equal(PixelKind.Balanced, v.Kind));
        Assert.All(p, v => Assert.InRange(v.A, 1, 254));
    }
    [Fact]
    public void SteepSupportedZeroCrossingNotMagnitudeCulled()
    {
        var f = new InfluenceBuffers(new MapGeometry(-4, -4, 8, 8, 8, 8));
        for (int z = 0; z < 8; z++)
            for (int x = 0; x < 8; x++)
            {
                f.Friendly[z * 8 + x] = 100 + 12 * (x - 4);
                f.Enemy[z * 8 + x] = 94;
            }
        var p = Rasterizer.Render(f, Settings(), 8, 8);
        Assert.Equal(PixelKind.Front, p[36].Kind);
    }
    [Fact]
    public void FactionSwapIsSymmetric()
    {
        var a = ScalarField.Compute(Snapshot(Stamp(), Stamp("b", Side.Enemy, -1, -1, 5)), Settings());
        var b = ScalarField.Compute(Snapshot(Stamp(side: Side.Enemy), Stamp("b", Side.Friendly, -1, -1, 5)), Settings());
        Assert.Equal(a.Friendly, b.Enemy);
        Assert.Equal(a.Enemy, b.Friendly);
    }
    [Fact]
    public void StableIdOrderIsDeterministicAndInputIsCopied()
    {
        var stamps = new[] { Stamp("z"), Stamp("a", weight: .5) };
        var s = Snapshot(stamps);
        stamps[0] = Stamp(weight: 100);
        var a = ScalarField.Compute(s, Settings());
        var b = ScalarField.Compute(Snapshot(Stamp("a", weight: .5), Stamp("z")), Settings());
        Assert.Equal(a.Friendly, b.Friendly);
    }
    [Fact]
    public void FarOffMapAndTinyRadiusRemainFinite()
    {
        var f = ScalarField.Compute(Snapshot(Stamp(x: 1e30)), Settings(radius: .001));
        Assert.All(f.Friendly, v => Assert.Equal(0, v));
    }
    [Fact]
    public void RectangularGeometryUsesBothCellSizes()
    {
        var g = new MapGeometry(100, 200, 16, 8, 8, 8);
        var s = new ObservationSnapshot(new SnapshotIdentity(1, 1, 1, 1, 1), g, new[] { Stamp(x: 109, z: 204.5) }, 0, 0, 15);
        var f = ScalarField.Compute(s, Settings());
        Assert.Equal(10, f.Friendly[36]);
        Assert.Equal(10 * (1 - 4.0 / 9), f.Friendly[37], 9);
    }
    [Fact]
    public void TranslationAwayFromClippingPreservesField()
    {
        var a = ScalarField.Compute(Snapshot(Stamp()), Settings());
        var s = new ObservationSnapshot(new SnapshotIdentity(1, 1, 1, 1, 1), new MapGeometry(96, 196, 8, 8, 8, 8), new[] { Stamp(x: 100.5, z: 200.5) }, 0, 0, 15);
        Assert.Equal(a.Friendly, ScalarField.Compute(s, Settings()).Friendly);
    }
    [Fact]
    public void ByteAlphaSurvivesSingleAirbase()
    {
        var p = Rasterizer.Render(ScalarField.Compute(Snapshot(Stamp()), Settings()), Settings(), 800, 800);
        Assert.InRange(p[36].A, 30, 50);
    }
}

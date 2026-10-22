using System.Text.Json.Nodes;
using FrontlineMap.Replay;
using Xunit;

public class LayeredReplayRegressionTests
{
    static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "LayeredFixtures", name + ".snapshot.json"));

    [Theory]
    [InlineData("source-point")][InlineData("off-center")][InlineData("stale-confidence")]
    [InlineData("land-barrier")][InlineData("blocked-corner")][InlineData("water")]
    [InlineData("unknown-strip")][InlineData("air-no-terrain")][InlineData("steep-strip")][InlineData("contested")][InlineData("dense")]
    public void IndependentReferenceMatchesProductionScheduler(string fixture)
    {
        var report = LayeredReplay.Compare(Fixture(fixture));
        Assert.True((bool)report.Pass);
        Assert.Equal(0, (int)report.PixelMismatches);
        Assert.Equal(5, (int)report.ComparedBuffers);
    }

    [Fact] public void OracleHasHandCheckedZeroPathAndStaleConfidenceValues()
    {
        var source = LayeredReplay.Compare(Fixture("source-point"));
        Assert.Equal(.09, source.ReferenceBuffers[0][0], 12);
        Assert.Equal(.09, source.ReferenceBuffers[2][0], 12);
        Assert.Equal(1, source.ReferenceBuffers[4][0]);
        var stale = LayeredReplay.Compare(Fixture("stale-confidence"));
        const int index = 3 * 64 + 3;
        Assert.Equal(1, stale.ReferenceBuffers[0][index]);
        Assert.Equal(1.5, stale.ReferenceBuffers[1][index]);
        Assert.Equal(6, stale.ReferenceBuffers[3][index]);
        Assert.Equal(2.5 / 7, stale.ReferenceBuffers[4][index], 12);
    }

    [Theory][InlineData("land-barrier")][InlineData("unknown-strip")][InlineData("steep-strip")]
    public void OracleBarriersHaveZeroSupportOnFarSide(string fixture)
    {
        var report = LayeredReplay.Compare(Fixture(fixture));
        Assert.True(report.ReferenceBuffers[0][1] > 0);
        Assert.Equal(0, report.ReferenceBuffers[0][4]);
    }

    [Fact] public void OracleWaterDomainExcludesLandAndDiagonalCannotCutBlockedCorner()
    {
        var water = LayeredReplay.Compare(Fixture("water"));
        Assert.Equal(.6, water.ReferenceBuffers[0][0], 12);
        Assert.Equal(0, water.ReferenceBuffers[0][4]);
        var corner = LayeredReplay.Compare(Fixture("blocked-corner"));
        Assert.Equal(0, corner.ReferenceBuffers[0][2 * 64 + 2]);
    }

    [Fact] public void GroundReplayRequiresExplicitTerrain()
    {
        var root = JsonNode.Parse(Fixture("off-center")); root.AsObject().Remove("terrain");
        Assert.Throws<ArgumentException>(() => LayeredReplay.Compare(root.ToJsonString()));
    }

    [Fact] public void SeparateTerrainFixtureAllowsObservationOnlyExport()
    {
        var root = JsonNode.Parse(Fixture("off-center")); string terrain = root["terrain"].ToJsonString(); root.AsObject().Remove("terrain");
        var report = LayeredReplay.Compare(root.ToJsonString(), terrain);
        Assert.True((bool)report.Pass);
    }

    [Theory][InlineData("count")][InlineData("bounds")][InlineData("nan")]
    public void MalformedTerrainIsRejected(string failure)
    {
        var root = JsonNode.Parse(Fixture("off-center"));
        if (failure == "count") root["terrain"]["known"].AsArray().RemoveAt(0);
        if (failure == "bounds") root["terrain"]["bounds"][2] = 32000;
        if (failure == "nan") root["terrain"]["heights"][0] = "NaN";
        Assert.Throws<ArgumentException>(() => LayeredReplay.Compare(root.ToJsonString()));
    }
}

using FrontlineMap.Core;
using Xunit;

public class TerrainCoverageTests
{
    [Theory]
    [InlineData(49, 100, true)]
    [InlineData(50, 100, false)]
    [InlineData(51, 100, false)]
    [InlineData(1, 3, true)]
    [InlineData(2, 3, false)]
    [InlineData(0, 100, true)]
    [InlineData(0, 0, true)]
    [InlineData(-1, 100, true)]
    [InlineData(101, 100, true)]
    public void CoverageUsesInclusiveHalfKnownBoundary(int known, int total, bool unavailable)
    {
        Assert.Equal(unavailable, TerrainCoverage.IsUnavailable(TacticalLayer.GroundControl, true, true, false, known, total));
    }
    [Theory]
    [InlineData(TacticalLayer.GroundControl)]
    [InlineData(TacticalLayer.Confidence)]
    [InlineData(TacticalLayer.NavalInfluence)]
    public void ConstrainedLayersReportSamplerFailure(TacticalLayer layer)
    {
        Assert.True(TerrainCoverage.IsUnavailable(layer, true, false, true, 100, 100));
    }
    [Theory]
    [InlineData(TacticalLayer.AirThreat, true)]
    [InlineData(TacticalLayer.Legacy, true)]
    [InlineData(TacticalLayer.GroundControl, false)]
    [InlineData(TacticalLayer.NavalInfluence, false)]
    [InlineData(TacticalLayer.Confidence, false)]
    public void RadialLayersAndExplicitOptOutBypassTerrainAvailability(TacticalLayer layer, bool enabled)
    {
        Assert.False(TerrainCoverage.IsUnavailable(layer, enabled, true, true, 0, 100));
    }
    [Fact] public void InProgressSamplingIsPendingRatherThanUnavailable()
    {
        Assert.False(TerrainCoverage.IsUnavailable(TacticalLayer.GroundControl, true, false, false, 0, 100));
    }
}

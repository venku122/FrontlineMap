using Xunit;
using FrontlineMap.Core;
public class ObservationPolicyTests
{
    [Theory]
    [InlineData(ViewerMode.Unresolved)]
    [InlineData(ViewerMode.Spectator)]
    [InlineData(ViewerMode.Unsupported)]
    public void UnresolvedAndSpectatorFailClosed(ViewerMode mode)
    {
        Assert.False(ObservationPolicy.TrySanitize(mode, 2, new[] { new NativeObservation("a", Side.Enemy, 1, 2, 5, 0, false, true) }, out var stamps, out _));
        Assert.Empty(stamps);
    }
    [Theory][InlineData(1)][InlineData(3)] public void UnsupportedFactionSetsFailClosed(int count) => Assert.False(ObservationPolicy.TrySanitize(ViewerMode.Assigned, count, Array.Empty<NativeObservation>(), out _, out _));
    [Fact]
    public void InvisibleAndLostNativeMarkersAreNotAdmitted()
    {
        Assert.True(ObservationPolicy.TrySanitize(ViewerMode.Assigned, 2, new[] { new NativeObservation("a", Side.Enemy, 1, 2, 5, 0, false, false) }, out var stamps, out _));
        Assert.Empty(stamps);
    }
    [Fact]
    public void StalePermittedPositionDoesNotRefreshFromHiddenWorld()
    {
        var visible = new NativeObservation("a", Side.Enemy, 12, 34, 5, 3, true, true);
        Assert.True(ObservationPolicy.TrySanitize(ViewerMode.Assigned, 2, new[] { visible }, out var first, out _)); // No authoritative state is accepted at this boundary.
        Assert.True(ObservationPolicy.TrySanitize(ViewerMode.Assigned, 2, new[] { visible }, out var second, out _));
        Assert.Equal(12, first[0].X);
        Assert.Equal(first[0].X, second[0].X);
        Assert.True(first[0].Stale);
        Assert.Equal(3, first[0].ObservedTime);
    }
}

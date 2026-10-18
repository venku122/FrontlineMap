using Xunit;
using FrontlineMap.Core;
using FrontlineMap.Runtime;
public class SchedulerTests
{
    [Fact]
    public void ChunkedCandidateMatchesIndependentFullGridOracle()
    {
        var s = ScalarFieldTests.Snapshot(ScalarFieldTests.Stamp(), ScalarFieldTests.Stamp("b", Side.Enemy, -1, -1, 5));
        var settings = ScalarFieldTests.Settings();
        using var q = new StepScheduler(() => 0);
        q.Begin(s, settings, 800, 800);
        q.Step(1);
        Assert.True(q.TryTakeCompleted(out var r));
        var o = ScalarField.Compute(s, settings);
        Assert.Equal(o.Friendly, r.Field.Friendly);
        Assert.Equal(o.Enemy, r.Field.Enemy);
        Assert.Equal(Rasterizer.Render(o, settings, 800, 800).Select(p => p.Kind), r.Pixels.Select(p => p.Kind));
    }
    [Fact]
    public void LargeStampChecksDeadlineWithinChunks()
    {
        double t = 0;
        using var q = new StepScheduler(() => t += .001);
        var s = new ObservationSnapshot(new SnapshotIdentity(1, 1, 1, 1, 1), new MapGeometry(-4, -4, 8, 8, 512, 512), new[] { ScalarFieldTests.Stamp() }, 0, 0, 15);
        q.Begin(s, ScalarFieldTests.Settings(512, 100), 800, 800);
        for (int step = 0; step < 5 && q.LastChunkCells == 0; step++)
            q.Step(t + .004);
        Assert.True(q.Busy);
        Assert.False(q.TryTakeCompleted(out _));
        Assert.InRange(q.LastChunkCells, 1, 128);
        q.Cancel();
        Assert.False(q.Busy);
    }
    [Fact]
    public void LatestPendingReplacesWithoutCancellingActive()
    {
        double t = 0;
        using var q = new StepScheduler(() => t += .001);
        var s = ScalarFieldTests.Snapshot();
        q.Begin(s, ScalarFieldTests.Settings(), 800, 800);
        for (int i = 2; i < 20; i++)
            q.Begin(new ObservationSnapshot(new SnapshotIdentity(1, 1, 1, 1, i), s.Geometry, s.Stamps, 0, 0, 15), ScalarFieldTests.Settings(), 800, 800);
        Assert.Equal(19, q.PendingSequence);
        q.Step(100);
        Assert.True(q.TryTakeCompleted(out var r));
        Assert.Equal(1, r.Snapshot.Identity.Sequence);
        q.Step(100);
        Assert.True(q.TryTakeCompleted(out r));
        Assert.Equal(19, r.Snapshot.Identity.Sequence);
    }
}
public class ExpiryAndRefreshTests
{
    [Fact]
    public void ExpiredActiveReleasesBudgetForFreshPending()
    {
        double now = 0;
        using var q = new StepScheduler(() => now);
        var s = ScalarFieldTests.Snapshot(ScalarFieldTests.Stamp());
        q.Begin(s, ScalarFieldTests.Settings(), 800, 800);
        var fresh = new ObservationSnapshot(new SnapshotIdentity(1, 1, 1, 1, 2), s.Geometry, s.Stamps, 14, 0, 29);
        q.Begin(fresh, ScalarFieldTests.Settings(), 800, 800);
        now = 16;
        q.Step(17);
        Assert.True(q.TryTakeCompleted(out var r));
        Assert.Equal(2, r.Snapshot.Identity.Sequence);
    }
    [Fact]
    public void RoutineContextRefreshDoesNotCancelActiveWork()
    {
        double now = 0;
        using var q = new StepScheduler(() => now += .001);
        var s = ScalarFieldTests.Snapshot(ScalarFieldTests.Stamp());
        q.Begin(s, ScalarFieldTests.Settings(), 800, 800);
        q.Step(.002);
        Assert.True(q.Busy);
        q.Step(1);
        Assert.True(q.TryTakeCompleted(out var r));
        Assert.Equal(1, r.Snapshot.Identity.Sequence);
    }
    [Fact]
    public void CoarseTexelsStillCoverBetweenSampleContour()
    {
        int n = 512;
        var g = new MapGeometry(-4, -4, 8, 8, n, n);
        var f = new InfluenceBuffers(g);
        for (int z = 0; z < n; z++)
            for (int x = 0; x < n; x++)
            {
                f.Friendly[z * n + x] = 100 + g.X(x);
                f.Enemy[z * n + x] = 100;
            }
        var p = Rasterizer.Render(f, ScalarFieldTests.Settings(n), 3600, 3600);
        Assert.Contains(p, v => v.Kind == PixelKind.Front && v.A > 0);
    }
}

using Xunit;
using FrontlineMap.Runtime;
using FrontlineMap.Diagnostics;
public class FaultTests
{
    [Theory]
    [InlineData(WorkStage.Allocation)]
    [InlineData(WorkStage.Stamp)]
    [InlineData(WorkStage.Raster)]
    public void ComputationFaultAtChunkBoundaryAbortsAndRecovers(WorkStage phase)
    {
        using var q = new StepScheduler(() => 0);
        q.ChunkCompleted = (stage, cells) => { if (stage == phase) throw new InvalidOperationException("Injected " + phase); };
        var s = ScalarFieldTests.Snapshot(ScalarFieldTests.Stamp());
        q.Begin(s, ScalarFieldTests.Settings(), 800, 800);
        Assert.Throws<InvalidOperationException>(() => q.Step(1));
        Assert.False(q.Busy);
        Assert.False(q.TryTakeCompleted(out _));
        q.ChunkCompleted = null;
        q.Begin(s, ScalarFieldTests.Settings(), 800, 800);
        q.Step(1);
        Assert.True(q.TryTakeCompleted(out var complete));
        Assert.Equal(10, complete.Field.Friendly[36]);
    }
}

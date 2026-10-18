using Xunit;
using FrontlineMap.Core;
using FrontlineMap.Runtime;
public class LifecycleTests
{
    sealed class Handle : IDisposable
    {
        public bool Disposed; public void Dispose() => Disposed = true;
    }
    sealed class View : IPresentation
    {
        public int Visible, Owned; public bool FailPrepare, FailCommit; public Action DuringPrepare; public IDisposable Prepare(FieldResult r)
        {
            if (FailPrepare)
                throw new InvalidOperationException("upload");
            Owned++;
            DuringPrepare?.Invoke();
            return new Handle();
        }
        public void Commit(IDisposable h)
        {
            if (FailCommit)
                throw new InvalidOperationException("commit");
            Visible++;
        }
        public void Clear()
        {
            Visible = 0;
        }
        public void Release(IDisposable h)
        {
            h.Dispose();
            Owned--;
        }
    }
    [Theory]
    [InlineData(InvalidationReason.Team)]
    [InlineData(InvalidationReason.Map)]
    [InlineData(InvalidationReason.Model)]
    [InlineData(InvalidationReason.Hidden)]
    public void InvalidContextCannotPublish(InvalidationReason why)
    {
        var v = new View();
        using var c = new OverlayController(v, () => 0);
        var s = ScalarFieldTests.Snapshot(ScalarFieldTests.Stamp());
        c.SetContext(s.Identity, 0, true);
        c.Submit(s, ScalarFieldTests.Settings(), 800, 800);
        c.Cancel(why);
        c.Tick(1, 10);
        Assert.Equal(0, v.Visible);
        Assert.False(c.Busy);
        Assert.Equal(0, v.Owned);
    }
    [Fact]
    public void ContextChangeAfterPrepareRejectsStaging()
    {
        var v = new View();
        using var c = new OverlayController(v, () => 0);
        var s = ScalarFieldTests.Snapshot();
        c.SetContext(s.Identity, 0, true);
        c.Submit(s, ScalarFieldTests.Settings(), 800, 800);
        c.BeforePublish = () => c.Cancel(InvalidationReason.Team);
        c.Tick(1, 10);
        Assert.Equal(0, v.Visible);
        Assert.Equal(0, v.Owned);
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UploadOrCommitFailureReleasesWorkAndRecovers(bool prepare)
    {
        var v = new View { FailPrepare = prepare, FailCommit = !prepare };
        using var c = new OverlayController(v, () => 0);
        var s = ScalarFieldTests.Snapshot();
        c.SetContext(s.Identity, 0, true);
        c.Submit(s, ScalarFieldTests.Settings(), 800, 800);
        c.Tick(1, 10);
        Assert.Equal(0, v.Visible);
        Assert.False(c.Busy);
        Assert.Equal(0, v.Owned);
        v.FailPrepare = false;
        v.FailCommit = false;
        c.Submit(s, ScalarFieldTests.Settings(), 800, 800);
        c.Tick(2, 10);
        Assert.Equal(1, v.Visible);
    }
    [Fact]
    public void ExpiredOutputClearsEvenWithoutNewWork()
    {
        var v = new View();
        using var c = new OverlayController(v, () => 0);
        var s = ScalarFieldTests.Snapshot();
        c.SetContext(s.Identity, 0, true);
        c.Submit(s, ScalarFieldTests.Settings(), 800, 800);
        c.Tick(1, 10);
        Assert.Equal(1, v.Visible);
        c.SetContext(s.Identity, 16, true);
        Assert.Equal(0, v.Visible);
        Assert.Equal(0, v.Owned);
    }
    [Fact]
    public void RepeatedEnableDisableReleasesEveryOwnedHandle()
    {
        var v = new View();
        using var c = new OverlayController(v, () => 0);
        var s = ScalarFieldTests.Snapshot();
        for (int i = 0; i < 25; i++)
        {
            c.SetContext(s.Identity, 0, true);
            c.Submit(s, ScalarFieldTests.Settings(), 800, 800);
            c.Tick(1, 10);
            c.Cancel(InvalidationReason.Hidden);
            Assert.Equal(0, v.Owned);
        }
    }

    [Fact]
    public void ExpiryDuringUploadPreparationPreventsCommit()
    {
        double now = 0;
        var v = new View { DuringPrepare = () => now = 16 };
        using var c = new OverlayController(v, () => now);
        var s = ScalarFieldTests.Snapshot();
        c.SetContext(s.Identity, 0, true);
        c.Submit(s, ScalarFieldTests.Settings(), 800, 800);
        c.Tick(1, 10);
        Assert.Equal(0, v.Visible);
        Assert.Equal(0, v.Owned);
    }
    [Fact]
    public void RoutineRefreshKeepsAuthorizedCompleteOutput()
    {
        var v = new View();
        using var c = new OverlayController(v, () => 0);
        var s = ScalarFieldTests.Snapshot();
        c.SetContext(s.Identity, 0, true);
        c.Submit(s, ScalarFieldTests.Settings(), 800, 800);
        c.Tick(1, 10);
        for (int i = 0; i < 20; i++)
            c.SetContext(s.Identity, 1 + i * .1, true);
        Assert.Equal(1, v.Visible);
        Assert.Equal(1, v.Owned);
    }
    [Fact]
    public void RecoverableFailureRetainsStillValidPreviousTexture()
    {
        var v = new View();
        using var c = new OverlayController(v, () => 0);
        var s = ScalarFieldTests.Snapshot();
        c.SetContext(s.Identity, 0, true);
        c.Submit(s, ScalarFieldTests.Settings(), 800, 800);
        c.Tick(1, 10);
        v.FailPrepare = true;
        var next = new ObservationSnapshot(new SnapshotIdentity(1, 1, 1, 1, 2), s.Geometry, s.Stamps, 1, 0, 16);
        c.Submit(next, ScalarFieldTests.Settings(), 800, 800);
        c.Tick(2, 10);
        Assert.Equal(1, v.Visible);
        Assert.Equal(1, v.Owned);
        Assert.Equal(1, c.Displayed.Snapshot.Identity.Sequence);
    }
}

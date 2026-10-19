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
    [Fact]
    public void CapturedCohortRevocationClearsDisplayedAndPendingWorkDuringPrepare()
    {
        var admissions = new AdmissionTracker();
        var a = new AdmissionToken(1, 10, 3, false, false);
        var b = new AdmissionToken(2, 10, 3, false, false);
        admissions.Begin(10); admissions.Add(a); admissions.Complete(); admissions.CommitCapture();
        SnapshotIdentity Identity(long sequence) => new SnapshotIdentity(1, 1, admissions.Revision, 1, sequence);
        var original = ScalarFieldTests.Snapshot(ScalarFieldTests.Stamp("a"));
        ObservationSnapshot Snapshot(long sequence, params ObservationStamp[] stamps) => new ObservationSnapshot(Identity(sequence), original.Geometry, stamps, 0, 0, 15);
        var view = new View();
        using var controller = new OverlayController(view, () => 0);
        controller.SetContext(Identity(1), 0, true);
        controller.Submit(Snapshot(1, ScalarFieldTests.Stamp("a")), ScalarFieldTests.Settings(), 800, 800);
        controller.Tick(0, 10);
        Assert.NotNull(controller.Displayed);
        long initialPolicy = admissions.Revision;
        admissions.Begin(10); admissions.Add(a); admissions.Add(b); admissions.Complete(); admissions.CommitCapture();
        Assert.Equal(initialPolicy, admissions.Revision);
        controller.SetContext(Identity(2), 0, true);
        var stamps = new[] { ScalarFieldTests.Stamp("a"), ScalarFieldTests.Stamp("b") };
        controller.Submit(Snapshot(2, stamps), ScalarFieldTests.Settings(), 800, 800);
        controller.Submit(Snapshot(3, stamps), ScalarFieldTests.Settings(), 800, 800);
        controller.BeforePublish = () =>
        {
            admissions.Begin(10); admissions.Add(b); admissions.Complete();
            controller.SetContext(Identity(3), 0, true);
            Assert.Null(controller.Displayed);
        };
        controller.Tick(0, 10);
        Assert.Null(controller.Displayed);
        Assert.False(controller.Busy);
        Assert.Equal(0, view.Owned);
        Assert.Equal(0, view.Visible);
        Assert.Equal(1, controller.RejectedCount);
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

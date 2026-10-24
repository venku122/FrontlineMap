using System.Reflection;
using FrontlineMap.Core;
using FrontlineMap.Runtime;
using Xunit;

public class KnownEstimateRefreshTests
{
    sealed class Handle : IDisposable
    {
        public int Releases;
        public void Dispose() => Releases++;
    }
    sealed class View : IPresentation
    {
        public readonly List<Handle> Handles = new List<Handle>();
        public Handle Visible;
        public bool FailPrepare, FailCommit;
        public IDisposable Prepare(FieldResult result)
        {
            if (FailPrepare) throw new InvalidOperationException("prepare");
            var handle = new Handle(); Handles.Add(handle); return handle;
        }
        public void Commit(IDisposable staged)
        {
            if (FailCommit) throw new InvalidOperationException("commit");
            Visible = (Handle)staged;
        }
        public void Clear() => Visible = null;
        public void Release(IDisposable handle) => handle.Dispose();
    }
    sealed class Fixture : IDisposable
    {
        public readonly View View = new View();
        public readonly OverlayController Controller;
        public readonly ObservationSnapshot Original;
        public double Now;
        public Fixture()
        {
            Controller = new OverlayController(View, () => Now);
            Original = Snapshot(1, 1);
            Controller.SetContext(Original.Identity, 0, true);
            Submit(Original); Controller.Tick(0, 10);
            Assert.NotNull(Controller.Displayed); Assert.Single(View.Handles);
        }
        public ObservationSnapshot Snapshot(long policy, long sequence, double expiry = 15)
        {
            var geometry = ScalarFieldTests.Snapshot().Geometry;
            return new ObservationSnapshot(Identity(policy, sequence), geometry,
                new[] { ScalarFieldTests.Stamp() }, 0, 0, expiry);
        }
        public void Submit(ObservationSnapshot snapshot) =>
            Controller.Submit(snapshot, ScalarFieldTests.Settings(), 800, 800);
        public void Revise(long policy, long sequence = 2) =>
            KnownEstimateRefreshTests.Revise(Controller, Identity(policy, sequence), Now, true);
        public void Dispose() => Controller.Dispose();
    }
    static SnapshotIdentity Identity(long policy, long sequence = 2, long session = 1, long map = 1, long model = 1) =>
        new SnapshotIdentity(session, map, policy, model, sequence);
    static void Revise(OverlayController controller, SnapshotIdentity identity, double now, bool sameViewer)
    {
        var method = typeof(OverlayController).GetMethod("ReviseKnowledge", new[] { typeof(SnapshotIdentity), typeof(double), typeof(bool) });
        Assert.NotNull(method);
        method.Invoke(controller, new object[] { identity, now, sameViewer });
    }
    static bool Retaining(OverlayController controller)
    {
        var property = typeof(OverlayController).GetProperty("RetainingPreviousKnowledge", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property); Assert.Equal(typeof(bool), property.PropertyType);
        return (bool)property.GetValue(controller);
    }

    [Fact] public void SameViewerPolicyRevisionRetainsShownAndCancelsActiveAndPendingOldWork()
    {
        using var f = new Fixture();
        var displayed = f.Controller.Displayed; var handle = f.View.Visible;
        f.Submit(f.Snapshot(1, 2)); f.Submit(f.Snapshot(1, 3));
        Assert.True(f.Controller.Busy);
        f.Revise(2);
        Assert.False(f.Controller.Busy); Assert.True(Retaining(f.Controller));
        Assert.Same(displayed, f.Controller.Displayed); Assert.Same(handle, f.View.Visible);
        f.Controller.Tick(1, 10);
        Assert.Equal(1, f.Controller.PublishedCount); Assert.Equal(0, handle.Releases);
        // A stale-policy submission also cannot restart canceled work.
        f.Submit(f.Snapshot(1, 4)); f.Controller.Tick(2, 10);
        Assert.False(f.Controller.Busy); Assert.Equal(1, f.Controller.PublishedCount);
    }
    [Fact] public void RevisionDuringBeforePublishRejectsStagingAndRetainsPreviousDisplay()
    {
        using var f = new Fixture(); var old = f.Controller.Displayed; var oldHandle = f.View.Visible;
        f.Submit(f.Snapshot(1, 2)); f.Submit(f.Snapshot(1, 3));
        f.Controller.BeforePublish = () => f.Revise(2);
        f.Controller.Tick(1, 10);
        Assert.True(Retaining(f.Controller)); Assert.Same(old, f.Controller.Displayed);
        Assert.Same(oldHandle, f.View.Visible); Assert.False(f.Controller.Busy);
        Assert.Equal(1, f.Controller.RejectedCount); Assert.Equal(1, f.Controller.PublishedCount);
        Assert.Equal(2, f.View.Handles.Count); Assert.Equal(1, f.View.Handles[1].Releases);
        Assert.Equal(0, oldHandle.Releases);
        f.Controller.Tick(2, 10); Assert.Equal(2, f.View.Handles.Count);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void RepeatedRevisionsAndPrepareFailureCannotExtendOriginalExpiry(bool failPrepare)
    {
        using var f = new Fixture(); var originalHandle = f.View.Visible;
        f.Now = 5; f.Revise(2);
        f.Now = 10; f.Revise(3, 3);
        if (failPrepare)
        {
            f.View.FailPrepare = true; f.Submit(f.Snapshot(3, 3, expiry: 100));
            f.Controller.Tick(10, 20); Assert.NotNull(f.Controller.LastError);
        }
        f.Now = 14.999; f.Revise(4, 4); f.Controller.Tick(f.Now, 20);
        Assert.True(Retaining(f.Controller)); Assert.Same(f.Original, f.Controller.Displayed.Snapshot);
        f.Now = 15; f.Controller.Tick(f.Now, 20);
        Assert.Null(f.Controller.Displayed); Assert.Null(f.View.Visible);
        Assert.False(Retaining(f.Controller)); Assert.Equal(1, originalHandle.Releases);
        f.Controller.Dispose(); Assert.Equal(1, originalHandle.Releases);
    }
    [Theory]
    [InlineData(false, 1, 1, 1)] [InlineData(true, 2, 1, 1)]
    [InlineData(true, 1, 2, 1)] [InlineData(true, 1, 1, 2)]
    public void ViewerSessionMapAndModelChangesHardClear(bool sameViewer, long session, long map, long model)
    {
        using var f = new Fixture(); var handle = f.View.Visible;
        f.Submit(f.Snapshot(1, 2));
        Revise(f.Controller, Identity(2, session: session, map: map, model: model), 1, sameViewer);
        Assert.Null(f.Controller.Displayed); Assert.Null(f.View.Visible);
        Assert.False(Retaining(f.Controller)); Assert.False(f.Controller.Busy);
        Assert.Equal(1, handle.Releases);
    }
    [Fact] public void ViewerGuardForcesCancellationEvenWhenAllIdentityValuesMatch()
    {
        using var f = new Fixture(); var handle = f.View.Visible;
        f.Submit(f.Snapshot(1, 2));
        Revise(f.Controller, f.Original.Identity, 1, false);
        Assert.Null(f.Controller.Displayed); Assert.Null(f.View.Visible);
        Assert.False(Retaining(f.Controller)); Assert.False(f.Controller.Busy);
        Assert.Equal(1, handle.Releases);
    }
    [Fact] public void InvalidPriorContextCannotRetainOrPublishOldPolicyKnowledge()
    {
        using var f = new Fixture();
        f.Controller.SetContext(Identity(1), 1, false);
        f.Revise(2);
        Assert.False(Retaining(f.Controller)); Assert.Null(f.Controller.Displayed);
        Assert.All(f.View.Handles, h => Assert.Equal(1, h.Releases));
    }
    [Fact] public void StrictSetContextStillClearsRetainedDisplayOnPolicyChange()
    {
        using var f = new Fixture(); var handle = f.View.Visible;
        f.Revise(2); Assert.True(Retaining(f.Controller));
        f.Controller.SetContext(Identity(3), 1, true);
        Assert.Null(f.Controller.Displayed); Assert.Null(f.View.Visible);
        Assert.False(Retaining(f.Controller)); Assert.Equal(1, handle.Releases);
    }
    [Fact] public void RetainedOldPolicyDisplayCannotBeRepainted()
    {
        using var f = new Fixture(); var original = f.Controller.Displayed;
        f.Revise(2);
        f.Controller.Repaint(ScalarFieldTests.Settings(), 800, 800);
        Assert.False(f.Controller.Busy);
        f.Controller.Tick(1, 10);
        Assert.Same(original, f.Controller.Displayed); Assert.Single(f.View.Handles);
        Assert.Equal(1, f.Controller.PublishedCount); Assert.True(Retaining(f.Controller));
    }
    [Fact] public void RevisionPreservesSequenceFloorAndSuccessfulReplacementReleasesOldOnce()
    {
        using var f = new Fixture(); var original = f.Controller.Displayed; var oldHandle = f.View.Visible;
        f.Revise(2);
        f.Submit(f.Snapshot(2, 1)); f.Controller.Tick(1, 10);
        Assert.Same(original, f.Controller.Displayed); Assert.True(Retaining(f.Controller));
        Assert.Single(f.View.Handles);
        f.Submit(f.Snapshot(2, 2, expiry: 30)); f.Controller.Tick(2, 10);
        Assert.False(Retaining(f.Controller)); Assert.Equal(2, f.Controller.Displayed.Snapshot.Identity.Policy);
        Assert.Equal(2, f.Controller.Displayed.Snapshot.Identity.Sequence);
        Assert.Equal(1, oldHandle.Releases); Assert.Equal(2, f.View.Handles.Count);
        var replacement = f.View.Visible; Assert.NotSame(oldHandle, replacement);
        f.Controller.Dispose(); f.Controller.Dispose();
        Assert.Equal(1, oldHandle.Releases); Assert.Equal(1, replacement.Releases);
    }
    [Fact] public void CommitFailureReleasesStagingButKeepsKnownEstimateUntilCancel()
    {
        using var f = new Fixture(); var old = f.Controller.Displayed; var handle = f.View.Visible;
        f.Revise(2); f.View.FailCommit = true;
        f.Submit(f.Snapshot(2, 2)); f.Controller.Tick(1, 10);
        Assert.NotNull(f.Controller.LastError); Assert.True(Retaining(f.Controller));
        Assert.Same(old, f.Controller.Displayed); Assert.Same(handle, f.View.Visible);
        Assert.Equal(0, handle.Releases); Assert.Equal(1, f.View.Handles[1].Releases);
        f.Controller.Cancel(InvalidationReason.Hidden);
        Assert.Null(f.Controller.Displayed); Assert.Null(f.View.Visible);
        Assert.False(Retaining(f.Controller)); Assert.Equal(1, handle.Releases);
        f.Controller.Dispose(); Assert.All(f.View.Handles, h => Assert.Equal(1, h.Releases));
    }
    [Fact] public void AlreadyExpiredDisplayCannotEnterRetainingMode()
    {
        using var f = new Fixture(); var handle = f.View.Visible;
        f.Now = 15; f.Revise(2);
        Assert.Null(f.Controller.Displayed); Assert.False(Retaining(f.Controller));
        Assert.Equal(1, handle.Releases);
    }
}

using FrontlineMap.Core;
using Xunit;
public class AdmissionTrackerTests
{
    static AdmissionToken Token(long id = 1, int owner = 10, double weight = 3, bool stale = false, bool airbase = false) => new AdmissionToken(id, owner, weight, stale, airbase);
    static long Scan(AdmissionTracker tracker, int viewer, params AdmissionToken[] tokens)
    { tracker.Begin(viewer); foreach (var token in tokens) tracker.Add(token); return tracker.Complete(); }
    [Fact] public void AdditionsDoNotRevokeAlreadyAuthorizedSnapshotInputs()
    { var t = new AdmissionTracker(); var first = Scan(t, 10, Token()); t.CommitCapture(); Assert.Equal(first, Scan(t, 10, Token(), Token(2))); }
    [Fact] public void RemovingAnUncapturedAdditionDoesNotRevoke()
    { var t = new AdmissionTracker(); var first = Scan(t, 10, Token()); t.CommitCapture(); Scan(t, 10, Token(), Token(2)); Assert.Equal(first, Scan(t, 10, Token())); }
    [Fact] public void CapturedAdditionsBecomePartOfTheRevocationBoundary()
    { var t = new AdmissionTracker(); Scan(t, 10, Token()); t.CommitCapture(); var first = Scan(t, 10, Token(), Token(2)); t.CommitCapture(); Assert.True(Scan(t, 10, Token()) > first); }
    [Fact] public void RemovingACapturedMarkerRevokesImmediately()
    { var t = new AdmissionTracker(); var first = Scan(t, 10, Token()); t.CommitCapture(); Assert.True(Scan(t, 10) > first); }
    [Fact] public void ViewerChangesInvalidateEvenAnEmptyMap()
    { var t = new AdmissionTracker(); var first = Scan(t, 10); Assert.True(Scan(t, 20) > first); }
    [Theory]
    [InlineData(2, 10, 3, false, false)]
    [InlineData(1, 20, 3, false, false)]
    [InlineData(1, 10, 4, false, false)]
    [InlineData(1, 10, 3, true, false)]
    [InlineData(1, 10, 3, false, true)]
    public void EveryCapturedAuthorizationChangeRevokes(long id, int owner, double weight, bool stale, bool airbase)
    { var t = new AdmissionTracker(); var first = Scan(t, 10, Token()); t.CommitCapture(); Assert.True(Scan(t, 10, Token(id, owner, weight, stale, airbase)) > first); }
    [Fact] public void ReorderingTheSameMarkersDoesNotCancelWork()
    { var t = new AdmissionTracker(); var first = Scan(t, 10, Token(), Token(2)); t.CommitCapture(); Assert.Equal(first, Scan(t, 10, Token(2), Token())); }
    [Fact] public void ClearCannotReuseTheOldAuthorizationRevision()
    { var t = new AdmissionTracker(); var first = Scan(t, 10, Token()); t.CommitCapture(); t.Clear(); Assert.True(Scan(t, 10, Token()) > first); }
    [Fact] public void RepeatedScansOfOneRevocationDoNotGenerateAnEndlessRevisionLoop()
    { var t = new AdmissionTracker(); Scan(t, 10, Token()); t.CommitCapture(); var revoked = Scan(t, 10); Assert.Equal(revoked, Scan(t, 10)); }
    [Fact] public void HashCollisionsCannotAuthorizeAReplacementMarker()
    { var t = new AdmissionTracker(); var first = Scan(t, 10, Token(0)); t.CommitCapture(); Assert.True(Scan(t, 10, Token(4294967297L)) > first); }
    [Fact] public void LatestCaptureStillProtectsOlderInFlightInputs()
    { var t = new AdmissionTracker(); Scan(t, 10, Token()); t.CommitCapture(); var first = Scan(t, 10, Token(), Token(2)); t.CommitCapture(); Assert.True(Scan(t, 10, Token(2)) > first); }
    [Fact] public void StableScansAllocateNothingAfterCapacityWarmup()
    { var t = new AdmissionTracker(); for (int frame = 0; frame < 10; frame++) { t.Begin(10); for (int i = 0; i < 1000; i++) t.Add(Token(i)); t.Complete(); t.CommitCapture(); } var before = System.GC.GetAllocatedBytesForCurrentThread(); for (int frame = 0; frame < 100; frame++) { t.Begin(10); for (int i = 0; i < 1000; i++) t.Add(Token(i)); t.Complete(); } Assert.Equal(0, System.GC.GetAllocatedBytesForCurrentThread() - before); }
}

using FrontlineMap.Core;
using Xunit;

public class ContactMemoryTests
{
    static NativeObservation Enemy(string id = "1", double at = 10, double x = 5, bool stale = false,
        Side side = Side.Enemy, double weight = 3, ObservationRole role = ObservationRole.Unknown) =>
        new NativeObservation(id, side, x, 8, weight, at, stale, true, role);
    static AdmissionToken Token(long id = 1, int owner = 20, double weight = 3, bool stale = false,
        bool airbase = false, ObservationRole role = ObservationRole.Unknown) =>
        new AdmissionToken(id, owner, weight, stale, airbase, role);

    [Fact] public void DefaultsAndSameViewerContextPreserveCopiedEnemyKnowledge()
    {
        var memory = new ContactMemory();
        Assert.Equal(4096, memory.Capacity); Assert.Equal(900, memory.MaximumAge);
        memory.BeginContext(10);
        var observed = Enemy(); memory.Remember(observed, Token(), 10);
        memory.BeginContext(10);
        Assert.True(memory.TryGet("1", out var retained));
        Assert.NotSame(observed, retained.Observation);
        Assert.Equal(5, retained.Observation.X); Assert.Equal(Token(), retained.Token);
        Assert.Single(memory.Contacts);
        // The next world observation cannot modify the already copied knowledge.
        observed = Enemy(x: 999);
        Assert.Equal(5, retained.Observation.X);
    }
    [Fact] public void ChangedContextAndClearRemoveAllKnowledgeAndResetTime()
    {
        var m = new ContactMemory(); m.BeginContext(1); m.Remember(Enemy(), Token(), 10);
        m.BeginContext(2); Assert.Equal(0, m.Count);
        m.Remember(Enemy(at: 1), Token(), 1); Assert.Equal(1, m.Count);
        m.Clear(); Assert.Empty(m.Contacts); Assert.False(m.TryGet("1", out _));
    }
    [Theory] [InlineData(true)] [InlineData(false)]
    public void SimulationRollbackClearsHistory(bool throughRemember)
    {
        var m = new ContactMemory(); m.Remember(Enemy(), Token(), 10);
        if (throughRemember) m.Remember(Enemy("2", 9), Token(2), 9); else m.Prune(9);
        Assert.False(m.TryGet("1", out _)); Assert.Equal(throughRemember ? 1 : 0, m.Count);
    }
    [Fact] public void StaleRecaptureCannotMoveOrRefreshAnObservation()
    {
        var m = new ContactMemory(maximumAge: 20); m.Remember(Enemy(), Token(), 10);
        m.Remember(Enemy(at: 10, x: 900, stale: true), Token(stale: true), 25);
        m.Remember(Enemy(at: 9, x: 800), Token(), 26);
        Assert.True(m.TryGet("1", out var retained));
        Assert.Equal(5, retained.Observation.X); Assert.Equal(10, retained.Observation.ObservedAt);
        Assert.False(retained.Observation.Stale);
        m.Prune(29.999); Assert.Equal(1, m.Count);
        m.Prune(30); Assert.Equal(0, m.Count);
    }
    [Fact] public void NewerReacquisitionReplacesOnlyOneEntry()
    {
        var m = new ContactMemory(maximumAge: 20); m.Remember(Enemy(), Token(), 10);
        m.Remember(Enemy(at: 15, x: 900), Token(), 15);
        Assert.Single(m.Contacts); Assert.True(m.TryGet("1", out var retained));
        Assert.Equal(900, retained.Observation.X);
        m.Prune(30); Assert.Equal(1, m.Count); m.Prune(35); Assert.Equal(0, m.Count);
    }
    [Fact] public void CapacityEvictsOldestInsertionAndCyclingIdsStayBounded()
    {
        var m = new ContactMemory(3);
        for (int i = 0; i < 10000; i++)
        {
            m.Remember(Enemy((i % 7).ToString(), i), Token(i % 7), i);
            Assert.InRange(m.Count, 1, 3);
            Assert.Equal(m.Count, m.Contacts.Count());
        }
        Assert.Equal(new[] { "1", "2", "3" }, m.Contacts.Select(c => c.Observation.Id));
        Assert.True(m.Remove("2")); Assert.False(m.Remove("2")); Assert.Equal(2, m.Count);
    }
    [Fact] public void ReplacementDoesNotCreateQueueTombstonesOrChangeInsertionOrder()
    {
        var m = new ContactMemory(2, maximumAge: 20000);
        m.Remember(Enemy("1", 0), Token(), 0); m.Remember(Enemy("2", 0), Token(2), 0);
        for (int i = 1; i < 10000; i++) m.Remember(Enemy("1", i), Token(), i);
        m.Remember(Enemy("3", 9999), Token(3), 9999);
        Assert.False(m.TryGet("1", out _)); Assert.Equal(2, m.Count);
    }
    [Fact] public void ZeroMaximumAgeDisablesRetention()
    {
        var m = new ContactMemory(maximumAge: 0); m.Remember(Enemy(), Token(), 10);
        Assert.Empty(m.Contacts);
    }
    [Theory] [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)] [InlineData(double.NegativeInfinity)] [InlineData(-1)] [InlineData(11)]
    public void InvalidOrFutureObservationTimeCannotEnterHistory(double at)
    {
        var m = new ContactMemory(); m.Remember(Enemy(at: at), Token(), 10); Assert.Equal(0, m.Count);
    }
    [Theory] [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)] [InlineData(double.NegativeInfinity)] [InlineData(-1)]
    public void InvalidCurrentTimeClearsHistory(double now)
    {
        var m = new ContactMemory(); m.Remember(Enemy(), Token(), 10); m.Prune(now); Assert.Equal(0, m.Count);
        m.Remember(Enemy(), Token(), now); Assert.Equal(0, m.Count);
    }
    [Fact] public void FriendlyAndMalformedObservationsCannotEnterHistory()
    {
        var m = new ContactMemory();
        m.Remember(Enemy(side: Side.Friendly), Token(), 10);
        m.Remember(null, Token(), 10); m.Remember(Enemy(id: ""), Token(), 10);
        m.Remember(Enemy(x: double.NaN), Token(), 10);
        m.Remember(Enemy(weight: double.PositiveInfinity), Token(), 10);
        m.Remember(Enemy(weight: 0), Token(weight: 0), 10);
        m.Remember(Enemy(), Token(weight: 4), 10);
        Assert.Equal(0, m.Count);
    }
    [Theory] [InlineData(2, 20, 3, false, ObservationRole.Unknown)]
    [InlineData(1, 30, 3, false, ObservationRole.Unknown)]
    [InlineData(1, 20, 4, false, ObservationRole.Unknown)]
    [InlineData(1, 20, 3, true, ObservationRole.Unknown)]
    [InlineData(1, 20, 3, false, ObservationRole.AirDefense)]
    public void SameTimestampAuthorizationReplacementSurvivesDisappearance(long id, int owner, double weight,
        bool airbase, ObservationRole role)
    {
        var m = new ContactMemory(); m.Remember(Enemy(), Token(), 10);
        var replacementToken = Token(id, owner, weight, airbase: airbase, role: role);
        m.Remember(Enemy(x: 25, weight: weight, role: role), replacementToken, 10);
        Assert.True(m.TryGet("1", out var retained));
        Assert.Equal(replacementToken, retained.Token);
        Assert.Equal(25, retained.Observation.X);
        Assert.Equal(role, retained.Observation.Role);
        // No more admissions: model native disappearance by aging the retained copies only.
        m.Prune(20);
        var historical = Assert.Single(m.Contacts);
        Assert.Equal(replacementToken, historical.Token);
        Assert.Equal(25, historical.Observation.X);
        Assert.Equal(10, historical.Observation.ObservedAt);
    }
    [Fact] public void DifferentAuthorizationCanHaveAnOlderValidCachedObservation()
    {
        var m = new ContactMemory(); m.Remember(Enemy(), Token(), 10);
        m.Remember(Enemy(at: 9, x: 25), Token(owner: 30), 10);
        Assert.True(m.TryGet("1", out var retained));
        Assert.Equal(9, retained.Observation.ObservedAt); Assert.Equal(25, retained.Observation.X);
    }
    [Theory] [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)]
    [InlineData(11)] [InlineData(-1)]
    public void InvalidAuthorizationReplacementRevokesOldCopyWithoutReinserting(double at)
    {
        var m = new ContactMemory(); m.Remember(Enemy(), Token(), 10);
        m.Remember(Enemy(at: at), Token(owner: 30), 10);
        Assert.False(m.TryGet("1", out _)); Assert.Empty(m.Contacts);
    }
    [Fact] public void LaterAuthorizedOwnershipChangeReplacesOldKnowledge()
    {
        var m = new ContactMemory(); m.Remember(Enemy(), Token(), 10);
        m.Remember(Enemy(at: 11), Token(owner: 30), 11);
        Assert.True(m.TryGet("1", out var retained)); Assert.Equal(30, retained.Token.Owner);
    }
    [Fact] public void AlreadyExpiredInputCannotBeRevivedByRecapture()
    {
        var m = new ContactMemory(maximumAge: 20); m.Remember(Enemy(), Token(), 30); Assert.Empty(m.Contacts);
    }
    [Theory] [InlineData(0, 900)] [InlineData(-1, 900)] [InlineData(1, -1)] [InlineData(1, double.NaN)] [InlineData(1, double.PositiveInfinity)]
    public void InvalidBoundsAreRejected(int capacity, double age) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContactMemory(capacity, age));
}

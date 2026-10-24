using FrontlineMap.Core;
using FrontlineMap.Runtime;
using Xunit;

public class ContactMemoryModelTests
{
    [Fact]
    public void LostEnemyHistoryFadesInIncrementalModelWhileFriendlySupportStaysStable()
    {
        var memory = new ContactMemory(maximumAge: 900);
        var role = ObservationRole.GroundVehicle;
        memory.Remember(new NativeObservation("enemy", Side.Enemy, 12.5, 12.5, 3, 0, false, true, role),
            new AdmissionToken(1, 20, 3, false, false, role), 0);
        var geometry = new MapGeometry(-100, -100, 200, 200, 8, 8);
        var settings = new ModelSettings(8, 6000, 5, .16, .01, 1e-9, 2, 15, 3,
            new TacticalSettings(TacticalLayer.GroundControl, terrainEnabled: false));
        foreach (var pair in new[] { (0d, 3d), (120d, 1.5d), (240d, .75d), (900d, 0d) })
        {
            memory.Prune(pair.Item1);
            var stamps = new List<ObservationStamp>
            { new ObservationStamp("friendly", Side.Friendly, 12.5, 12.5, 10, pair.Item1, false, role) };
            foreach (var retained in memory.Contacts)
            {
                var old = retained.Observation;
                stamps.Add(new ObservationStamp(old.Id, old.Side, old.X, old.Z, old.Weight,
                    old.ObservedAt, true, old.Role));
            }
            using var scheduler = new StepScheduler(() => 0);
            scheduler.Begin(new ObservationSnapshot(new SnapshotIdentity(1, 1, 1, 1, 1), geometry,
                stamps, 0, pair.Item1, 15), settings, 800, 800);
            for (int chunks = 0; chunks < 10000 && scheduler.Step(1) != StepOutcome.Complete; chunks++) { }
            Assert.True(scheduler.TryTakeCompleted(out var result));
            Assert.Equal(10, result.Field.Friendly[36], 10);
            Assert.Equal(pair.Item2, result.Field.Enemy[36], 10);
            Assert.Equal(pair.Item1 == 900 ? 10 : 13, result.Field.RawFriendly[36] + result.Field.RawEnemy[36], 10);
        }
    }
}

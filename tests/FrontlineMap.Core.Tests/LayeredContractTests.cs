using FrontlineMap.Core;
using Xunit;
public class LayeredContractTests
{
    [Fact] public void ObservationExposesRoleWithoutHiddenWorldState()
    {
        Assert.NotNull(typeof(ObservationStamp).GetField("Role"));
        Assert.NotNull(typeof(ModelSettings).GetField("Tactical"));
    }
}
public class RoleAdmissionContractTests
{
    [Fact] public void ChangedPublicRoleRevokesCapturedClassification()
    {
        var constructor=typeof(AdmissionToken).GetConstructor(new[]{typeof(long),typeof(int),typeof(double),typeof(bool),typeof(bool),typeof(ObservationRole)});
        Assert.NotNull(constructor);
        var a=(AdmissionToken)constructor.Invoke(new object[]{1L,10,3d,false,false,ObservationRole.GroundVehicle});
        var b=(AdmissionToken)constructor.Invoke(new object[]{1L,10,3d,false,false,ObservationRole.AirDefense});
        var t=new AdmissionTracker();t.Begin(10);t.Add(a);long first=t.Complete();t.CommitCapture();t.Begin(10);t.Add(b);Assert.True(t.Complete()>first);
    }
}

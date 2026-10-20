using System;
using System.Linq;
using FrontlineMap.Core;
using FrontlineMap.Runtime;
using Xunit;
public class LayeredModelTests
{
    static readonly MapGeometry G = new MapGeometry(-4,-4,8,8,8,8);
    static ObservationStamp S(string id, ObservationRole role, Side side=Side.Friendly, bool stale=false, double observed=0, double weight=10, double x=.5, double z=.5)
        => new ObservationStamp(id,side,x,z,weight,observed,stale,role);
    static ModelSettings Settings(TacticalLayer layer, bool terrain=false) => new ModelSettings(8,3,5,.4,.01,1e-9,2,15,3,new TacticalSettings(layer,terrain,128,3,3,2,4,3,4));
    static ObservationSnapshot Snap(ObservationStamp[] stamps, double sim=0, TerrainCostGrid terrain=null) => new ObservationSnapshot(new SnapshotIdentity(1,1,1,1,1),G,stamps,0,sim,15,terrain);
    static InfluenceBuffers Compute(ObservationSnapshot s,ModelSettings m)
    {
        var type=typeof(ModelSettings).Assembly.GetType("FrontlineMap.Core.TacticalField");
        Assert.NotNull(type);var method=type.GetMethod("Compute");Assert.NotNull(method);
        return (InfluenceBuffers)method.Invoke(null,new object[]{s,m});
    }
    [Theory]
    [InlineData(ObservationRole.Aircraft)] [InlineData(ObservationRole.Ship)] [InlineData(ObservationRole.Unknown)]
    public void AircraftShipsAndUnknownNeverClaimGround(ObservationRole role)
    { var f=Compute(Snap(new[]{S("u",role)}),Settings(TacticalLayer.GroundControl));Assert.All(f.Friendly,v=>Assert.Equal(0,v)); }
    [Fact] public void GroundVehicleContributesAtKnownPosition()
    { var f=Compute(Snap(new[]{S("g",ObservationRole.GroundVehicle)}),Settings(TacticalLayer.GroundControl));Assert.Equal(10,f.Friendly[36],10);Assert.Equal(10*(1-1d/9),f.Friendly[37],10); }
    [Fact] public void BuildingsHaveLimitedStrategicGroundContribution()
    { var f=Compute(Snap(new[]{S("b",ObservationRole.Building)}),Settings(TacticalLayer.GroundControl));Assert.Equal(1.5,f.Friendly[36],10);Assert.Equal(0,f.Friendly[39]); }
    [Theory] [InlineData(TacticalLayer.AirThreat,ObservationRole.Aircraft)] [InlineData(TacticalLayer.AirThreat,ObservationRole.AirDefense)] [InlineData(TacticalLayer.NavalInfluence,ObservationRole.Ship)]
    public void RolesEnterTheirOwnLayer(TacticalLayer layer,ObservationRole role)
    { Assert.Equal(10,Compute(Snap(new[]{S("u",role)}),Settings(layer)).Friendly[36],10); }
    [Theory] [InlineData(TacticalLayer.AirThreat,ObservationRole.Building)] [InlineData(TacticalLayer.NavalInfluence,ObservationRole.Aircraft)] [InlineData(TacticalLayer.NavalInfluence,ObservationRole.GroundVehicle)]
    public void UnrelatedRolesDoNotEnterThreatLayers(TacticalLayer layer,ObservationRole role)
    { Assert.All(Compute(Snap(new[]{S("u",role)}),Settings(layer)).Friendly,v=>Assert.Equal(0,v)); }
    [Fact] public void RoleRadiiChangeFootprint()
    { var a=Compute(Snap(new[]{S("a",ObservationRole.Aircraft)}),Settings(TacticalLayer.AirThreat));var g=Compute(Snap(new[]{S("g",ObservationRole.GroundVehicle)}),Settings(TacticalLayer.GroundControl));Assert.True(a.Friendly[39]>0);Assert.Equal(0,g.Friendly[39]); }
    [Fact] public void StaleHalfLifeScalesSupportAndConfidence()
    { var f=Compute(Snap(new[]{S("u",ObservationRole.GroundVehicle,stale:true)},120),Settings(TacticalLayer.GroundControl));Assert.Equal(5,f.Friendly[36],10);Assert.Equal(10,f.RawFriendly[36],10);Assert.Equal(.5,f.Confidence[36],10); }
    [Fact] public void ConfidenceDoesNotConfuseWeakFreshWithStrongStale()
    { var f=Compute(Snap(new[]{S("fresh",ObservationRole.GroundVehicle,weight:1),S("old",ObservationRole.GroundVehicle,Side.Enemy,true,0,10)},240),Settings(TacticalLayer.GroundControl));Assert.Equal(3.5/11,f.Confidence[36],10); }
    [Fact] public void FutureObservationCannotExceedFullConfidence()
    { var f=Compute(Snap(new[]{S("u",ObservationRole.GroundVehicle,stale:true,observed:100)},0),Settings(TacticalLayer.GroundControl));Assert.Equal(1,f.Confidence[36]); }
    [Fact] public void GroundTerrainBlocksDisconnectedIsland()
    { var heights=Enumerable.Repeat(1d,64).ToArray();for(int z=0;z<8;z++)heights[z*8+4]=-1;var t=new TerrainCostGrid(G,heights,Enumerable.Repeat(true,64).ToArray());var f=Compute(Snap(new[]{S("u",ObservationRole.GroundVehicle,x:-.5)},terrain:t),Settings(TacticalLayer.GroundControl,true));Assert.True(f.Friendly[35]>0);Assert.Equal(0,f.Friendly[37]); }
    [Fact] public void RequiredButUnavailableTerrainFailsConservatively()
    { Assert.All(Compute(Snap(new[]{S("u",ObservationRole.GroundVehicle)}),Settings(TacticalLayer.GroundControl,true)).Friendly,v=>Assert.Equal(0,v)); }
    [Fact] public void AirIgnoresLandWaterPathConstraint()
    { var t=new TerrainCostGrid(G,Enumerable.Repeat(-10d,64).ToArray(),Enumerable.Repeat(true,64).ToArray());Assert.Equal(10,Compute(Snap(new[]{S("u",ObservationRole.Aircraft)},terrain:t),Settings(TacticalLayer.AirThreat,true)).Friendly[36]); }
    [Fact] public void SelectedLayerSchedulerDoesNotFallBackToLegacy()
    { double clock=0;using var s=new StepScheduler(()=>clock+=.00001);s.Begin(Snap(new[]{S("a",ObservationRole.Aircraft)}),Settings(TacticalLayer.GroundControl),800,800);FieldResult r=null;for(int i=0;i<10000&&r==null;i++){s.Step(clock+.01);s.TryTakeCompleted(out r);}Assert.NotNull(r);Assert.All(r.Field.Friendly,v=>Assert.Equal(0,v)); }
    [Fact] public void ContestedBandIncludesNearBalance()
    { var m=Settings(TacticalLayer.GroundControl);var f=Compute(Snap(new[]{S("f",ObservationRole.GroundVehicle),S("e",ObservationRole.GroundVehicle,Side.Enemy,weight:9)}),m);Assert.Equal(PixelKind.Balanced,Rasterizer.Sample(f,m,36,800,800).Kind); }
    [Fact] public void ConfidenceViewHasItsOwnColorClassification()
    { var m=Settings(TacticalLayer.Confidence);var f=Compute(Snap(new[]{S("u",ObservationRole.GroundVehicle,stale:true)},240),m);Assert.Equal("Confidence",Rasterizer.Sample(f,m,36,800,800).Kind.ToString()); }
    [Fact] public void InvalidLayerSettingsRejectedBeforeUse()
    { Assert.Throws<ArgumentOutOfRangeException>(()=>new TacticalSettings((TacticalLayer)99));Assert.Throws<ArgumentOutOfRangeException>(()=>new TacticalSettings(groundRadius:0));Assert.Throws<ArgumentOutOfRangeException>(()=>new TacticalSettings(staleHalfLife:0));Assert.Throws<ArgumentOutOfRangeException>(()=>new TacticalSettings(contestedRatio:1.1));Assert.Throws<ArgumentOutOfRangeException>(()=>new TacticalSettings(minimumFrontConfidence:-.1));Assert.Throws<ArgumentOutOfRangeException>(()=>new TacticalSettings(terrainResolution:31)); }
    [Fact] public void UnknownRoleValueCannotEnterObservation()
    { Assert.Throws<ArgumentOutOfRangeException>(()=>S("u",(ObservationRole)99)); }
    [Fact] public void LegacyRemainsNumericallyIdentical()
    { var s=Snap(new[]{S("u",ObservationRole.Aircraft)});var m=Settings(TacticalLayer.Legacy);var f=Compute(s,m);Assert.Equal(ScalarField.Compute(s,m).Friendly,f.Friendly); }
}

public class LayeredSchedulerPresentationTests
{
 [Fact] public void DenseContourFallsBackToBandWithoutPartialLineAndRepaintRetainsConfidence()
 {
  const int n=128;var g=new MapGeometry(0,0,128,128,n,n);var snapshot=new ObservationSnapshot(new SnapshotIdentity(1,1,1,1,1),g,Array.Empty<ObservationStamp>(),0,0,15);
  var field=new InfluenceBuffers(g,true);
  for(int i=0;i<g.Count;i++){field.Friendly[i]=((i%n+i/n)%2==0)?3:1;field.Enemy[i]=4-field.Friendly[i];field.Confidence[i]=.9;}
  var settings=new ModelSettings(n,3,5,.16,.01,1e-9,2,15,3,new TacticalSettings(TacticalLayer.GroundControl,false));
  using var scheduler=new StepScheduler(()=>0);scheduler.ChunkCompleted=(_,cells)=>Assert.InRange(cells,0,128);
  scheduler.BeginRepaint(new FieldResult(snapshot,field,Array.Empty<Pixel>()),settings,800,800);
  Assert.Equal(StepOutcome.Complete,scheduler.Step(1));Assert.True(scheduler.TryTakeCompleted(out var r));
  Assert.True(r.ContourLimited);Assert.Empty(r.Contours);Assert.Same(field,r.Field);Assert.Equal(.9,r.Field.Confidence[0]);Assert.All(r.Pixels,p=>Assert.NotEqual(PixelKind.Front,p.Kind));
 }
 [Fact] public void MixedRadialContributionsAndConfidenceMatchIndependentSum()
 {
  var g=new MapGeometry(-4,-4,8,8,8,8);var stamps=new[]{new ObservationStamp("a",Side.Friendly,.5,.5,3,0,false,ObservationRole.GroundVehicle),new ObservationStamp("b",Side.Enemy,-.5,.5,6,0,true,ObservationRole.GroundVehicle)};
  var s=new ObservationSnapshot(new SnapshotIdentity(1,1,1,1,1),g,stamps,0,120,15);
  var settings=new ModelSettings(8,3,5,.16,.01,1e-9,2,15,3,new TacticalSettings(TacticalLayer.GroundControl,false,128,3));
  using var scheduler=new StepScheduler(()=>0);scheduler.Begin(s,settings,800,800);scheduler.Step(1);Assert.True(scheduler.TryTakeCompleted(out var r));
  for(int z=0;z<8;z++)for(int x=0;x<8;x++){double dx=g.X(x)-.5,dz=g.Z(z)-.5,f=3*Math.Max(0,1-(dx*dx+dz*dz)/9);dx=g.X(x)+.5;double rawE=6*Math.Max(0,1-(dx*dx+dz*dz)/9),e=rawE*.5;int i=z*8+x;Assert.Equal(f,r.Field.Friendly[i],10);Assert.Equal(e,r.Field.Enemy[i],10);Assert.Equal(f+rawE>0?(f+e)/(f+rawE):0,r.Field.Confidence[i],10);}
 }
}

using System;
using System.Linq;
using System.Text.Json;
using FrontlineMap.Core;
using FrontlineMap.Diagnostics;
using Xunit;
public class LayeredExportTests
{
 [Fact] public void LayeredExportDeclaresSettingsRolesAndTerrainReplayLimitation()
 {
  var g=new MapGeometry(0,0,8,8,8,8);
  var s=new ObservationSnapshot(new SnapshotIdentity(1,1,1,1,1),g,new[]{new ObservationStamp("a",Side.Friendly,.5,.5,3,0,true,ObservationRole.Aircraft)},1,120,16);
  var m=new ModelSettings(8,3,5,.16,.01,1e-9,2,15,3,new TacticalSettings(TacticalLayer.GroundControl));
  using var d=JsonDocument.Parse(SnapshotWriter.ToJson(s,m));var r=d.RootElement;
  Assert.Equal(2,r.GetProperty("schema").GetInt32());Assert.Equal("GroundControl",r.GetProperty("model").GetProperty("layer").GetString());
  Assert.Equal(120,r.GetProperty("model").GetProperty("stale_half_life").GetDouble());
  Assert.Equal("Aircraft",r.GetProperty("stamps")[0].GetProperty("role").GetString());
  Assert.Equal("observations_only; local terrain required for layered replay",r.GetProperty("replay_scope").GetString());
  Assert.False(r.TryGetProperty("terrain",out _));
 }
 [Fact] public void LegacyExportKeepsOriginalSchema()
 {
  var s=ScalarFieldTests.Snapshot(ScalarFieldTests.Stamp());
  using var d=JsonDocument.Parse(SnapshotWriter.ToJson(s,ScalarFieldTests.Settings()));
  Assert.Equal(1,d.RootElement.GetProperty("schema").GetInt32());Assert.False(d.RootElement.TryGetProperty("model",out _));Assert.False(d.RootElement.GetProperty("stamps")[0].TryGetProperty("role",out _));
 }
 [Fact] public void LayeredExtremeOriginsMatchFullGridRadialPredicate()
 {
  var g=new MapGeometry(1e15,-1e15,.125,.125,64,31);
  double radius=.01;var stamp=new ObservationStamp("u",Side.Friendly,g.X(0),g.Z(0),10,0,false,ObservationRole.GroundVehicle);
  var s=new ObservationSnapshot(new SnapshotIdentity(1,1,1,1,1),g,new[]{stamp},0,0,15);
  var m=new ModelSettings(64,radius,5,.16,.01,1e-9,2,15,3,new TacticalSettings(TacticalLayer.GroundControl,false,128,radius));
  var f=TacticalField.Compute(s,m);
  for(int z=0;z<g.Rows;z++)for(int x=0;x<g.Columns;x++){double dx=g.X(x)-stamp.X,dz=g.Z(z)-stamp.Z,d2=dx*dx+dz*dz;Assert.Equal(d2<radius*radius?10*(1-d2/(radius*radius)):0,f.Friendly[z*g.Columns+x]);}
 }
}

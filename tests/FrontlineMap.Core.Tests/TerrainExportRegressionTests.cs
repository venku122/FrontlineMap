using System.Linq;
using System.Text.Json;
using FrontlineMap.Core;
using FrontlineMap.Diagnostics;
using Xunit;
public class TerrainExportRegressionTests
{
 [Fact] public void ProductionExportIncludesSanitizedStaticTerrainForDeterministicReplay()
 {
  var g=new MapGeometry(0,0,8,8,8,8);var tg=new MapGeometry(0,0,8,8,2,2);
  var terrain=new TerrainCostGrid(tg,new[]{1d,double.NaN,-2d,20d},new[]{true,false,true,true},0,30);
  var s=new ObservationSnapshot(new SnapshotIdentity(1,1,1,1,1),g,new[]{new ObservationStamp("known",Side.Friendly,.5,.5,4,0,true,ObservationRole.Building)},0,120,15,terrain);
  var m=new ModelSettings(8,2000,5,.16,.01,1e-9,2,15,3,new TacticalSettings(TacticalLayer.GroundControl));
  using var d=JsonDocument.Parse(SnapshotWriter.ToJson(s,m));var root=d.RootElement;
  Assert.True(root.TryGetProperty("terrain",out var t));Assert.Equal(2,t.GetProperty("columns").GetInt32());Assert.Equal(30,t.GetProperty("max_slope_degrees").GetDouble());
  Assert.False(t.GetProperty("known")[1].GetBoolean());Assert.Equal(0,t.GetProperty("heights")[1].GetDouble());
  Assert.Equal("complete_layered_static_terrain",root.GetProperty("replay_scope").GetString());
 }
}

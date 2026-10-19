// Disposable bridge helper only. Register with the bridge's existing mutation guards.
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using Newtonsoft.Json.Linq;
using NuclearOption.Networking;
namespace NOAutomationBridge {
internal static class FrontlineMapNetworkTests {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object savedSettings, lag;
 static readonly string[] Settings={"DropChance","DropSinFrequency","Latency","LatencySinAmplitude","LatencySinFrequency","JitterSinAmplitude","JitterSinFrequency","ZagAmplitude","ZagFrequency"};
 static object Get(object o,string n)=>o==null?null:o.GetType().GetField(n,F)?.GetValue(o)??o.GetType().GetProperty(n,F)?.GetValue(o);
 static void Set(object o,string n,object value) {var f=o.GetType().GetField(n,F);if(f==null)throw new MissingFieldException(o.GetType().FullName,n);f.SetValue(o,value);}
 static object State() {var n=NetworkManagerNuclearOption.i;return new {pid=Process.GetCurrentProcess().Id,executable=Process.GetCurrentProcess().MainModule.FileName,server=n?.Server.Active,client=n?.Client.Active,serverFactory=Get(n?.Server,"SocketFactory")?.GetType().FullName,clientFactory=Get(n?.Client,"SocketFactory")?.GetType().FullName,settings=lag==null?null:Settings.ToDictionary(k=>k,k=>Get(Get(lag,"settings"),k)),settingsBackupPresent=savedSettings!=null,plugins=Chainloader.PluginInfos.Select(p=>new {guid=p.Key,name=p.Value.Metadata.Name,version=p.Value.Metadata.Version.ToString(),location=p.Value.Location,dependencies=p.Value.Dependencies.Select(d=>new {guid=d.DependencyGUID,flags=d.Flags.ToString()}).ToArray()}).ToArray()};}
 public static object Run(JObject q) {var n=NetworkManagerNuclearOption.i;var action=q?["action"]?.ToString()??"state";
 switch(action) {
 case "state":return State();
 case "disconnect":if(n.Server.Active)throw new InvalidOperationException("Disconnect is client-only");n.Stop(false);return new {requested=true};
 case "startHost": {
 if(n.Server.Active||n.Client.Active)throw new InvalidOperationException("Stop network before UDP rehost");
 var port=q?["gamePort"]?.Value<int>()??46901;if(port<1024||port>65535)throw new ArgumentOutOfRangeException("gamePort");
 var parser=typeof(MissionManager).Assembly.GetTypes().First(t=>t.Name=="CommandLineArgParser");
 var mission=(NuclearOption.SavedMission.Mission)parser.GetMethod("LoadMission",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,new object[]{q?["mission"]?.ToString()??"Escalation",false});
 MissionManager.SetMission(mission,false);n.StartHost(new HostOptions(SocketType.UDP,GameState.Multiplayer,mission.MapKey){UdpPort=port});return new{requested=true};}
 case "connect": {
 if(n.Server.Active||n.Client.Active)throw new InvalidOperationException("Stop client before reconnecting");
 // Literal loopback only. No external/public lobby transport is available here.
 var port=q?["gamePort"]?.Value<int>()??46901;if(port<1024||port>65535)throw new ArgumentOutOfRangeException("gamePort");
 bool fault=q?["fault"]?.Value<bool>()??false;
 if(fault) {
 lag=Get(n,"lagTransport");if(lag==null)throw new InvalidOperationException("Built-in LagSocketFactory unavailable");
 if(savedSettings!=null)throw new InvalidOperationException("Restore previous lag settings first");
 var original=Get(lag,"settings");if(original==null)throw new InvalidOperationException("Built-in LagSettings unavailable");
 var settings=Activator.CreateInstance(original.GetType());foreach(var k in Settings)Set(settings,k,Get(original,k));
 float latency=q?["latencySeconds"]?.Value<float>()??.12f,drop=q?["dropChance"]?.Value<float>()??.1f;
 if(latency<0||latency>.5f||drop<0||drop>.25f)throw new ArgumentOutOfRangeException("Bounded local lag settings");
 Set(settings,"Latency",latency);Set(settings,"DropChance",drop);Set(settings,"DropSinFrequency",1f);
 Set(settings,"LatencySinAmplitude",0f);Set(settings,"JitterSinAmplitude",0f);Set(settings,"ZagAmplitude",0f);savedSettings=original;Set(lag,"settings",settings);
 }
 try{n.StartClient(new ConnectOptions(fault?SocketType.LagUdp:SocketType.UDP,"127.0.0.1",port));}catch{Restore();throw;}
 return State();}
 case "restore":if(n.Client.Active&&!n.Server.Active)throw new InvalidOperationException("Disconnect client before restoring lag settings");Restore();return State();
 default:throw new ArgumentException("Unknown network test action "+action);
 }}
 static void Restore(){if(savedSettings!=null){Set(lag,"settings",savedSettings);savedSettings=null;lag=null;}}
}}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using NuclearOption.Networking;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;
namespace NOAutomationBridge {
internal static class FrontlineMapAcceptanceTests {
 static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
 static object P=>Chainloader.PluginInfos.TryGetValue("com.noms.frontlinemap",out var p)?p.Instance:null;
 static object Get(object o,string n)=>o==null?null:o.GetType().GetField(n,F)?.GetValue(o)??o.GetType().GetProperty(n,F)?.GetValue(o);
 static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
 static object[] Capture(){var a=new object[]{null,null,true};if(P==null || !(bool)Call(P,"TryRead",a))throw new InvalidOperationException("Capture unavailable");return ((Array)a[1]).Cast<object>().ToArray();}
 static object Stamp(Unit u)=>Capture().FirstOrDefault(x=>(string)Get(x,"Id")=="unit:"+u.persistentID);
 static void Reset(){if(P!=null)Call(P,"Reset");}
 static Unit PickEnemy(){GameManager.GetLocalHQ(out var h);var m=SceneSingleton<DynamicMap>.i;return UnitRegistry.allUnits.FirstOrDefault(u=>u!=null && !u.disabled && u.NetworkHQ!=null && u.NetworkHQ!=h && u is GroundVehicle && h.GetTrackingData(u.persistentID)==null && !m.mapIcons.OfType<UnitMapIcon>().Any(i=>i.unit==u));}
 static bool revokePatched,revokeNext;static int nativeRevocations;
 static bool patched,faultNext;static int faults; static string faultStage="Prepare";
 static void Inject(MethodBase __originalMethod){if(!faultNext || __originalMethod.Name!=faultStage)return;faultNext=false;faults++;throw new InvalidOperationException("FLM_ACCEPTANCE_EXPECTED_"+faultStage);}
 static void PatchFault(string stage){var t=P.GetType().Assembly.GetType("FrontlineMap.Rendering.RasterOverlayRenderer");new Harmony("flm.acceptance.fault."+stage).Patch(t.GetMethod(stage,F),new HarmonyMethod(typeof(FrontlineMapAcceptanceTests),nameof(Inject)));}
 public static object Run(JObject q){string action=q?["action"]?.ToString()??"state";
 switch(action){
 case "state": return State(q?["collect"]?.Value<bool>()??false);
 case "pairedReads": return PairedReads();
 case "instrumentOverhead": return InstrumentOverhead();
 case "displayStats": return DisplayStats();
 case "capture": return new{observations=Capture().Select(s=>new{id=Get(s,"Id"),side=Get(s,"Side"),x=Get(s,"X"),z=Get(s,"Z"),weight=Get(s,"Weight"),stale=Get(s,"Stale")}).ToArray()};
 case "stop": NetworkManagerNuclearOption.i.Stop(false);return new{requested=true};
 case "start": {
 var t=typeof(MissionManager).Assembly.GetTypes().First(x=>x.Name=="CommandLineArgParser");var mission=t.GetMethod("LoadMission",F).Invoke(null,new object[]{q?["mission"]?.ToString()??"Escalation",false});
 if(q?["map"]!=null)((NuclearOption.SavedMission.Mission)mission).MapKey=NuclearOption.SceneLoading.MapKey.GameWorldPrefab(q["map"].ToString());
 MissionManager.SetMission((NuclearOption.SavedMission.Mission)mission,false);
 NetworkManagerNuclearOption.i.StartHost(new HostOptions(NuclearOption.Networking.SocketType.Offline,GameState.SinglePlayer,((NuclearOption.SavedMission.Mission)mission).MapKey));return new{requested=true};}
 case "restart": typeof(MissionManager).GetMethod("RestartMission",F).Invoke(null,null);return new{requested=true};
 case "pause": TimeScaleManager.Scale=q?["scale"]?.Value<float>()??0;return new{scale=Time.timeScale};
 case "resolution": Screen.SetResolution(q["width"].Value<int>(),q["height"].Value<int>(),false);return new{requested=true};
 case "pan": {var m=SceneSingleton<DynamicMap>.i;typeof(DynamicMap).GetField("positionOffset",F).SetValue(m,new Vector2(q?["x"]?.Value<float>()??0,q?["y"]?.Value<float>()??0));return new{requested=true};}
 case "heading": {var aircraft=SceneSingleton<CombatHUD>.i.aircraft;if(aircraft==null)throw new Exception("No local aircraft");aircraft.transform.rotation=Quaternion.Euler(0,q["yaw"].Value<float>(),0);var body=aircraft.GetComponent<Rigidbody>();if(body!=null){body.rotation=aircraft.transform.rotation;body.angularVelocity=Vector3.zero;}return new{requested=true};}
 case "transform": {var m=SceneSingleton<DynamicMap>.i;var r=m.mapImage.GetComponent<RectTransform>();r.localRotation=Quaternion.Euler(0,0,q?["rotation"]?.Value<float>()??0);r.anchoredPosition=new Vector2(q?["panX"]?.Value<float>()??0,q?["panY"]?.Value<float>()??0);return new{success=true};}
 case "missionCatalog": {var summaries=new List<object>();foreach(var key in NuclearOption.SavedMission.MissionGroup.BuiltIn.GetMissions()){if(NuclearOption.SavedMission.MissionSaveLoad.TryLoad(key,out var mission,out var error))summaries.Add(new{name=mission.Name,map=mission.MapKey.ToString()});}return new{missions=summaries};}
 case "nativeKnowledge": {GameManager.GetLocalHQ(out var h);var m=SceneSingleton<DynamicMap>.i;return new{viewer=h?.faction.factionName,simulation=Time.timeSinceLevelLoad,contacts=m?.mapIcons.OfType<UnitMapIcon>().Where(i=>i!=null && i.unit!=null && i.iconImage!=null && i.gameObject.activeInHierarchy && i.iconImage.enabled && i.iconImage.color.a>0).Select(i=>new{id=i.unit.persistentID.ToString(),side=DynamicMap.GetFactionMode(i.unit.NetworkHQ,true).ToString(),observed=h?.GetTrackingData(i.unit.persistentID)?.Observed()}).ToArray()};}
 case "catalog": {var t=typeof(MissionManager).Assembly.GetTypes().First(x=>x.Name=="MapLoader");var loaders=Resources.FindObjectsOfTypeAll(t);return new{loaders=loaders.Select(l=>new{name=l.name,current=Get(l,"CurrentMap")?.ToString(),maps=((IEnumerable)Get(l,"Maps")).Cast<object>().Select(x=>new{name=Get(x,"MapName"),prefab=Get(x,"PrefabName")}).ToArray()}).ToArray()};}
 case "themes": {var t=typeof(MissionManager).Assembly.GetTypes().First(x=>x.Name=="ThemeManager");return new{names=t.GetMethod("GetThemeGroupNames",F).Invoke(null,null),current=t.GetMethod("GetCurrentThemeGroupIndex",F).Invoke(null,null)};}
 case "theme": {var t=typeof(MissionManager).Assembly.GetTypes().First(x=>x.Name=="ThemeManager");var theme=t.GetMethod("GetThemeGroupByIndex",F).Invoke(null,new object[]{q["index"].Value<int>()});t.GetMethod("SwitchToThemeGroup",F).Invoke(null,new[]{Get(theme,"Id")});return new{success=true};}
 case "raycast": return Raycast();
 case "knowledge": return Knowledge();
 case "hidden": return Hidden();
 case "ownership": return Ownership();
 case "hiddenDeath": return HiddenDeath();
 case "death": return Death();
 case "partialCreationFault": return ArmPartial();
 case "dirtyMaterialFault": return ArmDirty();
 case "revokeOnPrepare": {if(!revokePatched){var t=P.GetType().Assembly.GetType("FrontlineMap.Rendering.RasterOverlayRenderer");new Harmony("flm.acceptance.revoke").Patch(t.GetMethod("Prepare",F),new HarmonyMethod(typeof(FrontlineMapAcceptanceTests),nameof(RevokeBeforePrepare)));revokePatched=true;}revokeNext=true;return new{armed=true,nativeRevocations};}
 case "fault": {if(!patched){PatchFault("Prepare");PatchFault("Commit");patched=true;}faultStage=q?["stage"]?.ToString()??"Prepare";faultNext=true;Reset();return new{armed=true,faults};}
 default:throw new ArgumentException("Unknown acceptance action "+action);
 }}
 static bool partialPatched,partialNext;static int partialFaults,dirtyFaults;
 static void PartialCreationFault(){if(partialNext){partialNext=false;partialFaults++;throw new InvalidOperationException("FLM_EXPECTED_PARTIAL_UI_CREATION");}}
 static IEnumerable<CodeInstruction> PartialCommitBody(IEnumerable<CodeInstruction> instructions){foreach(var instruction in instructions){yield return instruction;if(instruction.operand is MethodInfo method && method.DeclaringType==typeof(Transform) && method.Name=="SetParent" && method.GetParameters().Length==2)yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Call,typeof(FrontlineMapAcceptanceTests).GetMethod(nameof(PartialCreationFault),F));}}
 static object ArmPartial(){if(!partialPatched){var t=P.GetType().Assembly.GetType("FrontlineMap.Rendering.RasterOverlayRenderer");new Harmony("flm.acceptance.partial-ui").Patch(t.GetMethod("Commit",F),transpiler:new HarmonyMethod(typeof(FrontlineMapAcceptanceTests).GetMethod(nameof(PartialCommitBody),F)));partialPatched=true;}partialNext=true;return new{armed=true,partialFaults};}
 static object ArmDirty(){var image=Resources.FindObjectsOfTypeAll<RawImage>().First(x=>x.name=="FrontlineMap influence" && x.enabled && x.texture!=null);bool once=true;image.RegisterDirtyMaterialCallback(()=>{if(once){once=false;dirtyFaults++;throw new InvalidOperationException("FLM_EXPECTED_DIRTY_MATERIAL_CALLBACK");}});return new{armed=true,dirtyFaults};}

 static int calibrationSink;
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)] static void CalibrationRead(){calibrationSink++;}
 static object InstrumentOverhead(){const int n=100000;for(int i=0;i<1000;i++)CalibrationRead();var watch=System.Diagnostics.Stopwatch.StartNew();for(int i=0;i<n;i++)CalibrationRead();watch.Stop();double plain=watch.Elapsed.TotalMilliseconds;
 var harmony=new Harmony("flm.acceptance.overhead");var target=typeof(FrontlineMapAcceptanceTests).GetMethod(nameof(CalibrationRead),F);harmony.Patch(target,new HarmonyMethod(typeof(FrontlineMapRuntimeTests).GetMethod("BeforeRead",F)),new HarmonyMethod(typeof(FrontlineMapRuntimeTests).GetMethod("AfterRead",F)));
 FrontlineMapRuntimeTests.Action("profileStop");watch.Restart();for(int i=0;i<n;i++)CalibrationRead();watch.Stop();double disabled=watch.Elapsed.TotalMilliseconds;
 FrontlineMapRuntimeTests.Action("profileReset");watch.Restart();for(int i=0;i<n;i++)CalibrationRead();watch.Stop();double enabled=watch.Elapsed.TotalMilliseconds;FrontlineMapRuntimeTests.Action("profileStop");harmony.UnpatchSelf();return new{calls=n,plainPerCallMs=plain/n,disabledInstrumentationPerCallMs=disabled/n,enabledInstrumentationPerCallMs=enabled/n,incrementalEnabledOverheadMs=(enabled-disabled)/n,scope="Native Mono no-op calibration of same Harmony prefix/postfix and Stopwatch/sample path; excludes real-method interaction, frame sampler, RPC and observer overhead"};}
 static object DisplayStats(){var result=Get(Get(P,"controller"),"Displayed");if(result==null)return new{success=false,reason="No displayed result"};var stamps=((IEnumerable)Get(Get(result,"Snapshot"),"Stamps")).Cast<object>().ToArray();var pixels=(Array)Get(result,"Pixels");int stride=Math.Max(1,pixels.Length/4096),sampled=0,nonzero=0;for(int i=0;i<pixels.Length;i+=stride){sampled++;if(Convert.ToInt32(Get(pixels.GetValue(i),"A"))>0)nonzero++;}var overlay=Resources.FindObjectsOfTypeAll<RawImage>().FirstOrDefault(x=>x.name=="FrontlineMap influence" && x.enabled && x.texture!=null);return new{success=true,admitted=stamps.Length,friendly=stamps.Count(x=>Get(x,"Side").ToString()=="Friendly"),enemy=stamps.Count(x=>Get(x,"Side").ToString()=="Enemy"),pixelCount=pixels.Length,pixelsSampled=sampled,nonzeroAlphaSampled=nonzero,textureWidth=overlay?.texture?.width,textureHeight=overlay?.texture?.height};}

 static object PairedReads(){
 var baselinePath=System.IO.Path.Combine(BepInEx.Paths.BepInExRootPath,"FrontlineMap-baseline.dll");
 var old=Assembly.Load(System.IO.File.ReadAllBytes(baselinePath));var current=P.GetType().Assembly;
 object Bind(Assembly a){var args=new object[]{512,null,null};if(!(bool)a.GetType("FrontlineMap.Game.MapBinding").GetMethod("TryCreate",F).Invoke(null,args))throw new Exception("Binding failed: "+args[2]);return args[1];}
 object Adapter(Assembly a)=>Activator.CreateInstance(a.GetType("FrontlineMap.Game.GameObservationAdapter"),new object[]{10d,5d,3d,.5d,4d});
 var ob=Bind(old);var cb=Bind(current);var oa=Adapter(old);var ca=Adapter(current);var om=oa.GetType().GetMethod("TryRead",F);var cm=ca.GetType().GetMethod("TryRead",F);
 var op=new object[]{ob,null,null,null};var cp=new object[]{cb,true,null,null,null};if(!(bool)om.Invoke(oa,op)||!(bool)cm.Invoke(ca,cp))throw new Exception("Initial capture failed");
 var os=Newtonsoft.Json.JsonConvert.SerializeObject(op[1]);var cs=Newtonsoft.Json.JsonConvert.SerializeObject(cp[2]);
 bool parity=os==cs;cp[1]=false;for(int i=0;i<10;i++){om.Invoke(oa,op);cm.Invoke(ca,cp);}
 var random=new System.Random(20261019);var trials=new List<object>();
 for(int trial=0;trial<3;trial++){double oldMs=0,currentMs=0;int oldGc=0,currentGc=0;var oldTimes=new List<double>();var currentTimes=new List<double>();for(int i=0;i<60;i++){bool reverse=random.Next(2)==0;for(int j=0;j<2;j++){bool baseline=(j==0)!=reverse;var startGc=GC.CollectionCount(0);var watch=System.Diagnostics.Stopwatch.StartNew();if(!(bool)(baseline?om.Invoke(oa,op):cm.Invoke(ca,cp)))throw new Exception("Read rejected");watch.Stop();if(baseline){oldMs+=watch.Elapsed.TotalMilliseconds;oldGc+=GC.CollectionCount(0)-startGc;oldTimes.Add(watch.Elapsed.TotalMilliseconds);}else{currentMs+=watch.Elapsed.TotalMilliseconds;currentGc+=GC.CollectionCount(0)-startGc;currentTimes.Add(watch.Elapsed.TotalMilliseconds);}}}oldTimes.Sort();currentTimes.Sort();trials.Add(new{trial=trial+1,readsPerVersion=60,baselineMeanMs=oldMs/60,candidateMeanMs=currentMs/60,baselineP99Ms=oldTimes[58],candidateP99Ms=currentTimes[58],baselineGc0=oldGc,candidateGc0=currentGc});}
 Reset();return new{success=parity,observations=((Array)op[1]).Length,identicalNativeSnapshot=parity,baselineAssembly=old.GetName().Version.ToString(),candidateAssembly=current.GetName().Version.ToString(),scope="Unity/Mono direct adapter reads on identical native state within one frame, randomized interleaving; excludes binding/capture/whole-frame scheduling costs. Candidate warm reads defer capture, baseline materializes each read.",trials};
 }

 static void RevokeBeforePrepare(object result){if(!revokeNext)return;var stamps=((IEnumerable)Get(Get(result,"Snapshot"),"Stamps")).Cast<object>().Select(x=>(string)Get(x,"Id")).ToHashSet();var u=UnitRegistry.allUnits.FirstOrDefault(x=>x!=null && !x.disabled && x is GroundVehicle && stamps.Contains("unit:"+x.persistentID));if(u==null)return;revokeNext=false;u.DisableUnit();nativeRevocations++;}
 static object Digest(){var stamps=Get(Get(Get(P,"controller"),"Displayed"),"Snapshot");stamps=Get(stamps,"Stamps");if(stamps==null)return null;var text=Newtonsoft.Json.JsonConvert.SerializeObject(stamps);using(var hash=System.Security.Cryptography.SHA256.Create())return new{count=((IEnumerable)stamps).Cast<object>().Count(),sha256=BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant()};}
 static object State(bool collect){if(collect){GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();}
 var nm=NetworkManagerNuclearOption.i;GameManager.GetLocalHQ(out var viewer);
 var overlays=Resources.FindObjectsOfTypeAll<RawImage>().Where(i=>i.name=="FrontlineMap influence").ToArray();
 var gfx=typeof(Profiler).GetMethod("GetAllocatedMemoryForGraphicsDriver",F);var units=UnitRegistry.allUnits;
 return new{scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,mission=MissionManager.CurrentMission?.Name,running=MissionManager.IsRunning,gameState=GameManager.gameState.ToString(),server=nm?.Server.Active,client=nm?.Client.Active,connections=nm?.Server.AuthenticatedPlayers.Count(),viewer=viewer?.faction.factionName,playerCount=Resources.FindObjectsOfTypeAll<NuclearOption.Networking.Player>().Count(p=>p.gameObject.scene.IsValid()),localPlayer=GameManager.GetLocalHQ(out var _),units=units.Count(),overlays=overlays.Length,textureBytes=overlays.Sum(i=>i.texture==null?0L:Profiler.GetRuntimeMemorySizeLong(i.texture)),managedBytes=GC.GetTotalMemory(false),nativeAnonymousRasterTextures=Resources.FindObjectsOfTypeAll<Texture2D>().Count(t=>string.IsNullOrEmpty(t.name) && t.format==TextureFormat.RGBA32 && t.width>=64 && t.height>=64),mapRotation=SceneSingleton<DynamicMap>.i?.mapImage?.transform.eulerAngles.ToString(),mapPosition=SceneSingleton<DynamicMap>.i?.mapImage?.transform.localPosition.ToString(),nativeAllocated=Profiler.GetTotalAllocatedMemoryLong(),nativeReserved=Profiler.GetTotalReservedMemoryLong(),graphicsDriverAllocated=gfx?.Invoke(null,null),processWorkingSet=System.Diagnostics.Process.GetCurrentProcess().WorkingSet64,width=Screen.width,height=Screen.height,dpi=Screen.dpi,scale=Time.timeScale,simulation=Time.timeSinceLevelLoad,realtime=Time.realtimeSinceStartup,shownTextureMatchesPresentation=overlays.Any(x=>x.enabled && x.texture!=null && x.texture==(Get(Get(Get(P,"controller"),"shown"),"Texture") as Texture)),unsafeWhiteImages=overlays.Count(x=>x.enabled && x.texture==null && x.canvas!=null && x.canvas.enabled),partialFaults,dirtyFaults,frame=Time.frameCount,faults,faultNext,nativeRevocations,revokeNext,displayedObservationDigest=Digest(),pluginVersion=P?.GetType().Assembly.GetName().Version.ToString(),pluginModuleId=P?.GetType().Assembly.ManifestModule.ModuleVersionId.ToString(),pluginEnabled=(P as Behaviour)?.enabled,plugin=FrontlineMapRuntimeTests.State()};}
 static object Raycast(){var m=SceneSingleton<DynamicMap>.i;var es=EventSystem.current;if(es==null)return new{success=false,reason="No event system"};int tested=0,hit=0;bool intercepted=false;
 foreach(var marker in m.mapIcons.OfType<UnitMapIcon>().Where(i=>i!=null && i.iconImage!=null && i.iconImage.raycastTarget && i.gameObject.activeInHierarchy)) {var canvas=marker.iconImage.canvas;var cam=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;var point=RectTransformUtility.WorldToScreenPoint(cam,marker.iconImage.transform.position);var background=Get(m,"mapBackground") as Image;if(point.x<0||point.y<0||point.x>Screen.width||point.y>Screen.height||background!=null && !RectTransformUtility.RectangleContainsScreenPoint(background.rectTransform,point,cam))continue;tested++;var hits=new List<RaycastResult>();es.RaycastAll(new PointerEventData(es){position=point},hits);if(hits.Any(h=>h.gameObject==marker.iconImage.gameObject))hit++;if(hits.Any(h=>h.gameObject.name=="FrontlineMap influence"))intercepted=true;}
 return new{success=hit>0&&!intercepted,visibleMarkersTested=tested,nativeMarkersHit=hit,overlayIntercepted=intercepted,scope="Native Unity EventSystem raycasts at visible native marker centers; not physical mouse input"};}
 static void UpdateMarker(Unit u){var m=SceneSingleton<DynamicMap>.i;if(m.TryGetIcon(u,out var icon))icon.UpdateIcon(m.mapDisplayFactor,1f/m.mapImage.transform.localScale.x,m.mapImage.transform,DynamicMap.mapMaximized);}
 static object Knowledge(){GameManager.GetLocalHQ(out var h);var u=PickEnemy();if(u==null)return new{success=false,reason="No hidden enemy"};var origin=u.transform.position;var body=u.GetComponent<Rigidbody>();try{
 bool hidden=Stamp(u)==null;h.RpcUpdateTrackingInfo(u.persistentID);UpdateMarker(u);var fresh=Stamp(u);var tracking=h.GetTrackingData(u.persistentID);var known=tracking.GetPosition();tracking.lastKnownPosition=known;tracking.lastSpottedTime=Time.timeSinceLevelLoad-5f;
 u.transform.position=origin+new Vector3(17000,0,-11000);if(body!=null)body.position=u.transform.position;UpdateMarker(u);var stale=Stamp(u);bool held=stale!=null && fresh!=null && (double)Get(stale,"X")== (double)Get(fresh,"X") && (double)Get(stale,"Z")== (double)Get(fresh,"Z");
 h.DeregisterTrackedUnit(u);var lost=Stamp(u);h.RpcUpdateTrackingInfo(u.persistentID);UpdateMarker(u);var reacquired=Stamp(u);
 return new{success=hidden && fresh!=null && (bool)Get(stale,"Stale") && held && lost==null && reacquired!=null,hiddenRejected=hidden,freshAdmitted=fresh!=null,staleAdmitted=stale!=null,staleFlag=Get(stale,"Stale"),stalePositionHeld=held,lostRejected=lost==null,reacquired=reacquired!=null,scope="Controlled native tracking RPC, native TrackingInfo age threshold and native marker UpdateIcon within one frame; not complete real sensor flight"};
 }finally{u.transform.position=origin;if(body!=null)body.position=origin;h.DeregisterTrackedUnit(u);Reset();}}
 static object Hidden(){GameManager.GetLocalHQ(out var h);var u=PickEnemy();if(u==null)return new{success=false,reason="No hidden enemy"};var before=Newtonsoft.Json.JsonConvert.SerializeObject(Capture());var position=u.transform.position;var owner=u.NetworkHQ;var def=u.definition;try{u.transform.position+=new Vector3(19000,0,9000);u.NetworkHQ=null;u.definition=UnitRegistry.allUnits.First(x=>x!=null && x.definition!=null && x.definition!=def).definition;var after=Newtonsoft.Json.JsonConvert.SerializeObject(Capture());return new{success=before==after,unchanged=before==after,scope="Unrepresented, untracked unit true position/neutral ownership/definition changed at fixed frame; serialized admitted observations compared"};}finally{u.transform.position=position;u.NetworkHQ=owner;u.definition=def;Reset();}}
 static object HiddenDeath(){var u=PickEnemy();if(u==null)return new{success=false,reason="No hidden enemy"};var a=Newtonsoft.Json.JsonConvert.SerializeObject(Capture());u.DisableUnit();var b=Newtonsoft.Json.JsonConvert.SerializeObject(Capture());Reset();return new{success=a==b,unchanged=a==b,disabled=u.disabled,scope="Native death of untracked unrepresented enemy at fixed knowledge/time"};}
 static object Ownership(){GameManager.GetLocalHQ(out var h);var u=PickEnemy();if(u==null)return new{success=false,reason="No hidden enemy"};var owner=u.NetworkHQ;try{h.RpcUpdateTrackingInfo(u.persistentID);UpdateMarker(u);var before=Stamp(u);u.NetworkHQ=h;UpdateMarker(u);var after=Stamp(u);return new{success=before!=null && after!=null && Get(before,"Side").ToString()=="Enemy" && Get(after,"Side").ToString()=="Friendly",before=Get(before,"Side"),after=Get(after,"Side"),scope="Native NetworkHQ sync hook and faction callback"};}finally{u.NetworkHQ=owner;h.DeregisterTrackedUnit(u);Reset();}}
 static object Death(){GameManager.GetLocalHQ(out var h);var m=SceneSingleton<DynamicMap>.i;var u=m.mapIcons.OfType<UnitMapIcon>().Select(i=>i.unit).FirstOrDefault(x=>x!=null && !x.disabled && x is GroundVehicle && x.NetworkHQ==h);if(u==null)return new{success=false,reason="No friendly death fixture"};bool admitted=Stamp(u)!=null;u.DisableUnit();var after=Stamp(u);Reset();return new{success=admitted && u.disabled && after==null,admittedBefore=admitted,disabled=u.disabled,removed=after==null,scope="Native server DisableUnit and sync/event path in disposable mission"};}
}}

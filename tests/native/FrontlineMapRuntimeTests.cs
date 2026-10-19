using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
namespace NOAutomationBridge {
internal static class FrontlineMapRuntimeTests {
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object Get(object o,string n) => o==null?null:o.GetType().GetField(n,Flags)?.GetValue(o)??o.GetType().GetProperty(n,Flags)?.GetValue(o);
 static object PluginInstance => Chainloader.PluginInfos.TryGetValue("com.noms.frontlinemap",out var p)?p.Instance:null;
 static Func<long> Allocated;
 static bool profiling, patched, jamming;
 static double totalMs,maxMs; static long count,bytes; static readonly double[] samples=new double[30000]; static int startGc;
 struct Sample { public long Time,Bytes; }
 public static object State() {
 var p=PluginInstance; var c=Get(p,"controller"); var r=Get(p,"renderer"); var displayed=Get(c,"Displayed"); var snap=Get(displayed,"Snapshot");
 var overlay=Resources.FindObjectsOfTypeAll<RawImage>().FirstOrDefault(i=>i.name=="FrontlineMap influence");
 var m=SceneSingleton<DynamicMap>.i;
 var masks=m?.mapImage?.GetComponentsInParent<RectMask2D>(true).Select(x=>new{name=x.name,enabled=x.enabled,active=x.gameObject.activeInHierarchy,rect=x.rectTransform.rect.ToString(),scale=x.transform.lossyScale.ToString()}).ToArray();
 var icons=m?.mapIcons.OfType<UnitMapIcon>().Where(i=>i!=null && i.iconImage!=null).Take(3).Select(i=>new{id=i.unit?.persistentID,root=i.transform.name,image=i.iconImage.transform.name,sameTransform=i.transform==i.iconImage.transform,local=i.iconImage.transform.localPosition.ToString(),rootScale=i.transform.localScale.ToString(),imageParent=i.iconImage.transform.parent?.name,active=i.gameObject.activeInHierarchy,imageActive=i.iconImage.gameObject.activeInHierarchy}).ToArray();
 var stamps=Get(snap,"Stamps") as IEnumerable;
 return new {pluginPresent=p!=null,visible=Get(p,"visible"),bindingFailure=Get(p,"lastFailure"),geometry=Get(p,"geometryKey"),policy=Get(p,"policyRevision"),sequence=Get(p,"sequence"),published=Get(c,"PublishedCount"),busy=Get(c,"Busy"),rejected=Get(c,"RejectedCount"),ownedTextures=Get(r,"OwnedTextures"),ownedObjects=Get(r,"OwnedObjects"),displayed=snap!=null,displayedSequence=Get(Get(snap,"Identity"),"Sequence"),stampCount=stamps?.Cast<object>().Count(),overlay=overlay==null?null:new{enabled=overlay.enabled,active=overlay.gameObject.activeInHierarchy,texture=overlay.texture!=null?overlay.texture.name:null,texturePresent=overlay.texture!=null,rect=overlay.rectTransform.rect.ToString(),scale=overlay.transform.lossyScale.ToString(),canvas=overlay.canvas?.name,layer=overlay.gameObject.layer},mapAllowed=DynamicMap.AllowedToOpen,mapMaximized=DynamicMap.mapMaximized,masks,icons,gc0=GC.CollectionCount(0),managedBytes=GC.GetTotalMemory(false)};
 }
 public static object HiddenMovement() {
 var m=SceneSingleton<DynamicMap>.i;
 if(m==null || !GameManager.GetLocalHQ(out var viewer) || m.HQ!=viewer)return new{success=false,reason="Unresolved viewer"};
 var represented=new HashSet<Unit>(m.mapIcons.OfType<UnitMapIcon>().Where(i=>i!=null && i.unit!=null).Select(i=>i.unit));
 Unit hidden=null;
 foreach(var unit in UnitRegistry.allUnits)if(unit!=null && unit.NetworkHQ!=null && unit.NetworkHQ!=viewer && !represented.Contains(unit) && viewer.GetTrackingData(unit.persistentID)==null && (unit is GroundVehicle || unit is Building || unit is Ship)){hidden=unit;break;}
 if(hidden==null)return new{success=false,reason="No eligible hidden enemy fixture"};
 var p=PluginInstance;var read=p.GetType().GetMethod("TryRead",Flags);var before=new object[]{null,null,true};
 if(!(bool)read.Invoke(p,before))return new{success=false,reason="Capture unavailable"};
 string a=Newtonsoft.Json.JsonConvert.SerializeObject(before[1]);var position=hidden.transform.position;var body=hidden.GetComponent<Rigidbody>();
 try {hidden.transform.position=position+new Vector3(17000,0,-11000);if(body!=null)body.position=hidden.transform.position;
 var after=new object[]{null,null,true};if(!(bool)read.Invoke(p,after))return new{success=false,reason="Second capture unavailable"};
 string b=Newtonsoft.Json.JsonConvert.SerializeObject(after[1]);
 return new{success=a==b,scope="Untracked unrepresented enemy moved in one native frame; sanitized observations compared",admitted=((Array)before[1]).Length,unchanged=a==b,simulationTime=Time.timeSinceLevelLoad};
 }finally{hidden.transform.position=position;if(body!=null)body.position=position;}
 }
 public static object Audit() {
 var p=PluginInstance; var snapshot=Get(Get(Get(p,"controller"),"Displayed"),"Snapshot");
 if(snapshot==null)return new{success=false,reason="No committed overlay"};
 var geometry=Get(snapshot,"Geometry");var rect=SceneSingleton<DynamicMap>.i.mapImage.GetComponent<RectTransform>();
 var overlay=Resources.FindObjectsOfTypeAll<RawImage>().FirstOrDefault(i=>i.name=="FrontlineMap influence" && i.enabled && i.texture!=null);
 if(overlay==null)return new{success=false,reason="No committed overlay"};
 var m=SceneSingleton<DynamicMap>.i;var native=new Dictionary<string,MapIcon>();
 foreach(var marker in m.mapIcons)if(marker is UnitMapIcon icon && icon.unit!=null && icon.iconImage!=null && icon.gameObject.activeInHierarchy && icon.iconImage.enabled && icon.iconImage.color.a>0)native["unit:"+icon.unit.persistentID]=icon;
 foreach(var icon in m.GetComponentsInChildren<AirbaseMapIcon>(true))if(icon.airbase!=null && icon.iconImage!=null && icon.gameObject.activeInHierarchy && icon.iconImage.enabled && icon.iconImage.color.a>0)native["airbase:"+icon.airbase.GetInstanceID()]=icon;
 var canvas=rect.GetComponentInParent<Canvas>();var camera=canvas!=null && canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
 int admitted=0,missing=0;double maximumError=0; var bounds=overlay.rectTransform.rect;
 foreach(var stamp in (IEnumerable)Get(snapshot,"Stamps")) { admitted++;var id=(string)Get(stamp,"Id");if(!native.TryGetValue(id,out var marker)){missing++;continue;}
 var local=marker.iconImage.transform.localPosition;double x=local.x/m.mapDisplayFactor,z=local.y/m.mapDisplayFactor;
 double u=(x-(double)Get(geometry,"MinX"))/(double)Get(geometry,"Width"),v=(z-(double)Get(geometry,"MinZ"))/(double)Get(geometry,"Height");
 var projected=overlay.transform.TransformPoint(new Vector3((float)(bounds.xMin+u*bounds.width),(float)(bounds.yMin+v*bounds.height),0));
 var a=RectTransformUtility.WorldToScreenPoint(camera,projected);var b=RectTransformUtility.WorldToScreenPoint(camera,marker.iconImage.transform.position);
 maximumError=Math.Max(maximumError,Vector2.Distance(a,b)); }
 return new{success=missing==0 && maximumError<.1,admitted,missingNativeMarkers=missing,maxLandmarkErrorPixels=maximumError,raycastTarget=overlay.raycastTarget,overlaySibling=overlay.transform.GetSiblingIndex(),zoom=m.GetZoomLevel(),mapMaximized=DynamicMap.mapMaximized};
 }
 public static object Action(string action) {
 var p=PluginInstance;
 if(action=="audit")return Audit();
 if(action=="hiddenMovement")return HiddenMovement();
 if(action=="zoomIn") {SceneSingleton<DynamicMap>.i.SetZoomLevel(3f);return State();}
 if(action=="zoomOut") {SceneSingleton<DynamicMap>.i.SetZoomLevel(1f);return State();}
 if(action=="toggle") { var f=p.GetType().GetField("visible",Flags);f.SetValue(p,!(bool)f.GetValue(p));p.GetType().GetMethod("Reset",Flags).Invoke(p,null); }
 else if(action=="jamOn") jamming=true;
 else if(action=="jamOff") { jamming=false; if(SceneSingleton<CombatHUD>.i!=null) SceneSingleton<CombatHUD>.i.jamAccumulation=0; }
 else if(action=="spectate") {var menu=LegacyUiCompatibility.FindObject("LeaderboardMenu");menu?.GetType().GetMethod("OnSpectateClicked",Flags)?.Invoke(menu,null);}
 else if(action=="close") DynamicMap.EnableCanvas(false);
 else if(action=="open") { var menu=LegacyUiCompatibility.FindObject("LeaderboardMenu"); if(menu!=null)menu.GetType().GetMethod("Close",Flags)?.Invoke(menu,null); SceneSingleton<GameplayUI>.i.CloseJoinMenu(); SceneSingleton<DynamicMap>.i.Maximize(); }
 else if(action=="profileReset") {
 if(!patched) { var method=typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread",BindingFlags.Static|BindingFlags.Public);if(method!=null) { Allocated=(Func<long>)Delegate.CreateDelegate(typeof(Func<long>),method);var before=Allocated();var probe=new byte[1024];GC.KeepAlive(probe);if(Allocated()<=before)Allocated=null; }var h=new Harmony("frontlinemap.local-test-profiler");h.Patch(p.GetType().GetMethod("TryRead",Flags),new HarmonyMethod(typeof(FrontlineMapRuntimeTests),nameof(BeforeRead)),new HarmonyMethod(typeof(FrontlineMapRuntimeTests),nameof(AfterRead)));h.Patch(p.GetType().GetMethod("LateUpdate",Flags),new HarmonyMethod(typeof(FrontlineMapRuntimeTests),nameof(ForceJam)));patched=true; }
 count=0;bytes=0;totalMs=0;maxMs=0;startGc=GC.CollectionCount(0);profiling=true;
 }
 else if(action=="profileStop") profiling=false;
 else if(action=="profileState") {var a=samples.Take((int)Math.Min(count,samples.Length)).OrderBy(x=>x).ToArray();return new {reads=count,totalMs,meanMs=count==0?0:totalMs/count,maxMs,p99Ms=a.Length==0?0:a[(int)((a.Length-1)*.99)],allocatedBytes=Allocated==null?(long?)null:bytes,bytesPerRead=Allocated==null||count==0?(double?)null:(double)bytes/count,gc0=GC.CollectionCount(0)-startGc,instrumentation="Harmony prefixes around complete Plugin.TryRead; overhead not subtracted"};}
 return State();
 }
 static void BeforeRead(out Sample __state) {__state=profiling?new Sample{Time=Stopwatch.GetTimestamp(),Bytes=Allocated?.Invoke()??0}:default;}
 static void AfterRead(Sample __state) { if(!profiling||__state.Time==0)return; var elapsed=(Stopwatch.GetTimestamp()-__state.Time)*1000.0/Stopwatch.Frequency;bytes+=(Allocated?.Invoke()??0)-__state.Bytes;totalMs+=elapsed;maxMs=Math.Max(maxMs,elapsed);samples[count%samples.Length]=elapsed;count++; }
 static void ForceJam() {Tick();}
 public static void Tick() {if(jamming && SceneSingleton<CombatHUD>.i!=null)SceneSingleton<CombatHUD>.i.jamAccumulation=1;}
}}

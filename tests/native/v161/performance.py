from native_checks import *
import csv,math,statistics,uuid,random
PM=Path(os.environ['FLM_PRESENTMON']).resolve()
SCRIPT=HERE/'benchmark-process.ps1'
EXPECTED=hashlib.sha256((REPO/'bin/Release/net472/FrontlineMap.dll').read_bytes()).hexdigest()
assert hashlib.sha256((PROFILE/'BepInEx/plugins/FrontlineMap.dll').read_bytes()).hexdigest()==EXPECTED
criteria=dict(pausedMeanFrameRatioMax=1.05,pausedP99RatioMax=1.20,pausedP99AbsoluteAllowanceMs=3,liveMeanPresentRateMin=55,liveP99FrameMaxMs=33.333,liveOver50msPercentMax=.5,nativeReadMeanMaxMs=2,nativeReadP99MaxMs=4,liveAvailabilityMinPercent=80,liveLongestRecomputeGapMaxSeconds=5)
record('performance-plan',dict(binarySha256=EXPECTED,criteria=criteria,design='Three balanced pairs, 20 seconds per condition, same paused mission/camera/full map, overlay visible vs F9-equivalent off. Then 120 seconds live GroundControl with per-frame visibility/read probes. PresentMon same exact guarded PID; no hardware/graphics changes.',limits=['Paused comparison isolates overlay costs, not live AI load.','Live simulation evolves; live data are not an equal-workload baseline comparison.','Frame cap can hide reduced CPU/GPU headroom; busy and process CPU are also reported.','Live Harmony instrumentation adds overhead. No general FPS improvement claim.']))
def percentile(a,p):
 a=sorted(a);return a[min(len(a)-1,math.ceil(len(a)*p)-1)] if a else None
def measure(label,seconds):
 n=rpc('debug.frontlineMapNetwork',{'action':'state'});pid=n['pid'];assert n['executable'].lower()==win(PROFILE/'NuclearOption.exe').lower(),n
 folder=OUT/label;folder.mkdir(exist_ok=True)
 args=[PWSH,'-NoProfile','-File',win(SCRIPT),'-Action','measure','-ExpectedExe',win(PROFILE/'NuclearOption.exe'),'-GameProcessId',str(pid),'-DurationSeconds',str(seconds),'-PresentMon',win(PM),'-CsvPath',win(folder/'presentmon.csv'),'-ResultPath',win(folder/'process.json'),'-SessionName','FLMverify'+uuid.uuid4().hex[:16]]
 r=subprocess.run(args,capture_output=True,text=True,timeout=seconds+35)
 (folder/'collector.log').write_text(r.stdout+r.stderr);assert r.returncode==0,(r.stdout+r.stderr)[-2000:]
 with (folder/'presentmon.csv').open(encoding='utf-8-sig',newline='') as f:rows=[x for x in csv.DictReader(f) if int(x['ProcessID'])==pid]
 assert len(rows)>seconds*20,len(rows)
 from collections import Counter
 chain=Counter(x['SwapChainAddress'] for x in rows).most_common(1)[0][0];rows=[x for x in rows if x['SwapChainAddress']==chain]
 metrics={}
 for k in ['MsBetweenPresents','MsBetweenDisplayChange','MsCPUBusy','MsGPUTime']:
  v=[float(x[k]) for x in rows if x.get(k) not in [None,'','NA'] and float(x[k])>0];metrics[k]=dict(n=len(v),mean=statistics.mean(v) if v else None,p50=percentile(v,.5),p95=percentile(v,.95),p99=percentile(v,.99),over50msPercent=100*sum(x>50 for x in v)/len(v) if v else None)
 d=json.loads((folder/'process.json').read_text(encoding='utf-8-sig'));samples=d['samples'];cpu=(samples[-1]['cpuSeconds']-samples[0]['cpuSeconds'])/(samples[-1]['elapsedSeconds']-samples[0]['elapsedSeconds'])
 data=dict(label=label,durationSeconds=seconds,frames=len(rows),presentRate=1000/metrics['MsBetweenPresents']['mean'],metrics=metrics,cpuCoreEquivalent=cpu,workingSetFirst=samples[0]['workingSetBytes'],workingSetLast=samples[-1]['workingSetBytes'],workingSetPeak=max(x['workingSetBytes'] for x in samples),pid=pid)
 record(label+'-summary',data);print(label,json.dumps(data),flush=True);return data

def visible(on):
 s=ac('state')['plugin']
 if s['visible']!=on:rpc('debug.frontlineMapTest',{'action':'toggle'})
 if on:wait(shown,90)
 else:wait(lambda:not layer()['displayed'] and layer()['ownedTextures']==0,30)
 time.sleep(5)

start('Escalation');select('GroundControl');ac('pause',scale=0);rpc('debug.frontlineMapTest',{'action':'zoomIn'});time.sleep(2);wait(shown,90)
base=record('paused-fixture',dict(native=ac('state'),layered=layer()))
trials=[]
try:
 for i,order in enumerate([(True,False),(False,True),(True,False)],1):
  for on in order:
   visible(on);before=ac('state');d=measure('paused-pair'+str(i)+('-on' if on else '-off'),20);after=ac('state');assert before['simulation']==after['simulation'],(before['simulation'],after['simulation']);d.update(pair=i,overlay=on,before=before,after=after);trials.append(d);record('paused-trials',dict(trials=trials))
 visible(True);ac('pause',scale=1);wait(shown,90);rpc('debug.frontlineMapTest',{'action':'profileReset'});ac('probeStart');before=ac('state')
 layer('frameProbeStart'); live=measure('live-ground',120); record('update-aligned-frames',layer('frameProbeStop'))
 live.update(before=before,after=ac('state'),layered=layer(),readProfile=rpc('debug.frontlineMapTest',{'action':'profileState'}),availability=ac('probeStop'));rpc('debug.frontlineMapTest',{'action':'profileStop'});record('live-result',live)
 means={};p99={}
 for on in [True,False]:
  group=[x for x in trials if x['overlay']==on];means[on]=statistics.median(x['metrics']['MsBetweenPresents']['mean'] for x in group);p99[on]=statistics.median(x['metrics']['MsBetweenPresents']['p99'] for x in group)
 a=live['availability'];availability=100*a['displayFrames']/(a['displayFrames']+a['hiddenFrames']);p=live['readProfile'];frames=live['metrics']['MsBetweenPresents']
 checks=dict(pausedMean=means[True]/means[False]<=criteria['pausedMeanFrameRatioMax'],pausedP99=(p99[True]/p99[False]<=criteria['pausedP99RatioMax'] or p99[True]-p99[False]<=criteria['pausedP99AbsoluteAllowanceMs']),liveMean=live['presentRate']>=criteria['liveMeanPresentRateMin'],liveP99=frames['p99']<=criteria['liveP99FrameMaxMs'],liveStutters=frames['over50msPercent']<=criteria['liveOver50msPercentMax'],nativeReadMean=p['meanMs']<=criteria['nativeReadMeanMaxMs'],nativeReadP99=p['p99Ms']<=criteria['nativeReadP99MaxMs'],availability=availability>=criteria['liveAvailabilityMinPercent'],recomputeGap=a['longestHiddenSeconds']<=criteria['liveLongestRecomputeGapMaxSeconds'])
 record('performance-result',dict(success=all(checks.values()),checks=checks,criteria=criteria,pausedMedianFrameMs={'on':means[True],'off':means[False]},pausedMedianP99Ms={'on':p99[True],'off':p99[False]},pausedMeanRatio=means[True]/means[False],live=live,availabilityPercent=availability,binarySha256=EXPECTED));print('PERFORMANCE',checks,flush=True)
finally:
 visible(True);ac('pause',scale=1);ac('probeStop');rpc('debug.frontlineMapTest',{'action':'profileStop'})

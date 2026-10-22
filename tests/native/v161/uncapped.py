from native_checks import *
exec((HERE/'performance.py').read_text().split("start('Escalation');select('GroundControl')")[0].replace("record('performance-plan',", "record('uncapped-base-plan',"))
SCRIPT=HERE/'benchmark-process.ps1'
criteria=dict(topThreadCoreEquivalentMin=.85,meanFrameRatioMax=1.10,p99FrameAllowanceMs=4,cpuToGpuBusyRatioMin=1.25,minimumPresentRate=90)
record('uncapped-plan',dict(binarySha256=EXPECTED,criteria=criteria,scope='Three balanced20-second pairs, paused same mission/full map;640x400 and Unity targetFrameRate=-1/vsync0. CPU-bound classification additionally requires >90FPS, observed busy ratio and a process thread consuming>=0.85core; flags alone do not prove uncapped execution. Graphics/resolution/pacing restored.'))
ac('pause',scale=0);select('GroundControl');prior=layer('pacing',rate=-1,vsync=0);ac('resolution',width=640,height=400);time.sleep(2);wait(shown,90);trials=[]
try:
 for i,order in enumerate([(True,False),(False,True),(True,False)],1):
  for on in order:
   visible(on);d=measure('uncapped-pair'+str(i)+('-on' if on else '-off'),20);proc=json.loads((OUT/d['label']/'process.json').read_text(encoding='utf-8-sig'));d['topThreadCoreEquivalent']=proc['topThreads'][0]['coreEquivalent'] if proc.get('topThreads') else None;d.update(overlay=on,pair=i);trials.append(d);record('uncapped-trials',dict(trials=trials))
 means={on:statistics.median(t['metrics']['MsBetweenPresents']['mean'] for t in trials if t['overlay']==on) for on in [True,False]}
 p99={on:statistics.median(t['metrics']['MsBetweenPresents']['p99'] for t in trials if t['overlay']==on) for on in [True,False]}
 cpuBound=all(t.get('topThreadCoreEquivalent') is not None and t['topThreadCoreEquivalent']>=criteria['topThreadCoreEquivalentMin'] and t['presentRate']>criteria['minimumPresentRate'] and t['metrics']['MsCPUBusy']['mean'] is not None and t['metrics']['MsGPUTime']['mean'] is not None and t['metrics']['MsCPUBusy']['mean']/t['metrics']['MsGPUTime']['mean']>criteria['cpuToGpuBusyRatioMin'] for t in trials)
 checks=dict(meanFrame=means[True]/means[False]<=criteria['meanFrameRatioMax'],p99=p99[True]-p99[False]<=criteria['p99FrameAllowanceMs'],cpuBoundDemonstrated=cpuBound)
 record('uncapped-result',dict(success=all(checks.values()),checks=checks,pausedMedianFrameMs={str(k):v for k,v in means.items()},pausedMedianP99Ms={str(k):v for k,v in p99.items()},trials=trials,binarySha256=EXPECTED))
 print('Uncapped checks',checks,flush=True)
finally:
 visible(True);layer('pacing',rate=prior['priorRate'],vsync=prior['priorVsync']);ac('resolution',width=1280,height=800);time.sleep(2);wait(shown,90);ac('pause',scale=1)

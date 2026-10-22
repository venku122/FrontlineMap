from guards import validate
validate(client=True,performance=True)
from native_checks import *
exec((HERE/'performance.py').read_text().split("start('Escalation');select('GroundControl')")[0].replace("record('performance-plan',", "record('dense-relative-base-plan',"))
exec((HERE/'network_smoke.py').read_text().split("ac('stop');wait")[0])
criteria=dict(meanFrameRatioMax=1.05,p99RatioMax=1.20,p99AllowanceMs=3)
record('dense-relative-plan',dict(binarySha256=EXPECTED,criteria=criteria,scope='Follow-up diagnosis after the predeclared50FPS absolute dense criterion failed. Two counterbalanced20-second on/off pairs per actual full/cockpit view, live same two-process UDP scene, continuous production map motion. Live AI evolves; no exact-input baseline or statistical guarantee. Relative result cannot erase the original absolute criterion failure.'))
ac('stop');wait(lambda:(lambda x:x['scene']=='MainMenu' and x['server'] is False and x['client'] is False)(ac('state')),60);nr('startHost',mission='Escalation');wait(ready,100);openmap()
args=[PWSH,'-NoProfile','-File',win(HERE/'launch.ps1'),'-GameRoot',win(CLIENT),'-MenuOnly','-LogFile',win(OUT/'dense-relative-client-player.log')];r=subprocess.run(args,check=True,capture_output=True,text=True);record('dense-relative-client-launch',json.loads(r.stdout));wait(lambda:(lambda x:x['scene']=='MainMenu' and x['server'] is False and x['client'] is False)(get(42681)),90);nr('connect',42681);wait(clientReady,100)
verify_running(CLIENT,42681)
for port,faction in [(42680,0),(42681,1)]:rpc('control.selectTeam',{'faction':faction},port=port);rpc('debug.frontlineMapTest',{'action':'open'},port=port)
wait(shown,90);wait(clientShown,90);ac('pause',scale=1);pilot=rpc('ai.spawnAtAirbaseWithControl',{'airbase':'airbase_city','aircraft':'CI-22','emptyLoadout':True});assert pilot['success'],pilot;time.sleep(2);trials=[]
try:
 for maximized in [True,False]:
  layer('mapView',maximized=maximized);time.sleep(1);wait(shown,90)
  for pair,order in enumerate([(True,False),(False,True)],1):
   for on in order:
    visible(on);before=ac('state');motion=layer('motionStart',mode='rigid',production=True)
    try:d=measure('dense-relative-'+str(maximized)+'-'+str(pair)+('-on' if on else '-off'),20);moving=layer('contourStats');assert moving['observedAngleTravel']>360,moving
    finally:layer('motionStop')
    d.update(overlay=on,maximized=maximized,pair=pair,before=before,after=ac('state'));trials.append(d);record('dense-relative-trials',dict(trials=trials))
 results=[]
 for maximized in [True,False]:
  means={on:statistics.median(t['metrics']['MsBetweenPresents']['mean'] for t in trials if t['overlay']==on and t['maximized']==maximized) for on in [True,False]};p99={on:statistics.median(t['metrics']['MsBetweenPresents']['p99'] for t in trials if t['overlay']==on and t['maximized']==maximized) for on in [True,False]}
  checks=dict(meanFrame=means[True]/means[False]<=criteria['meanFrameRatioMax'],p99=p99[True]/p99[False]<=criteria['p99RatioMax'] or p99[True]-p99[False]<=criteria['p99AllowanceMs']);results.append(dict(maximized=maximized,checks=checks,success=all(checks.values()),onFPS=1000/means[True],offFPS=1000/means[False],meanFrameRatio=means[True]/means[False],onP99=p99[True],offP99=p99[False]))
 record('dense-relative-result',dict(success=all(r['success'] for r in results),binarySha256=EXPECTED,criteria=criteria,cases=results));print('Dense relative',json.dumps(results),flush=True)
finally:
 visible(True);layer('motionStop');stop(42681,CLIENT);start('Escalation');ac('pause',scale=1);wait(shown,90)

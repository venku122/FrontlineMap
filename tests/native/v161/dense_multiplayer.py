from guards import validate
validate(client=True,performance=True)
from native_checks import *
exec((HERE/'performance.py').read_text().split("start('Escalation');select('GroundControl')")[0].replace("record('performance-plan',", "record('dense-base-plan',"))
criteria=dict(minimumUnits=1000,minimumAdmitted=300,players=2,livePresentRateMin=50,p99FrameMaxMs=33.333,over50msPercentMax=.5,availabilityMinPercent=80,maximumGapSeconds=5)
record('dense-multiplayer-plan',dict(binarySha256=EXPECTED,criteria=criteria,scope='Two actual loopback UDP players, live Escalation, >=1000 mission units and >=300 actually admitted host contacts. Continuous native full/cockpit map transform changes with production plugin enabled and periodic real captures; no analytic contours. 20seconds per view, no off baseline or claim for more than two players. Harmony/frame observer overhead included.'))
ac('pause',scale=1);pilot=rpc('ai.spawnAtAirbaseWithControl',{'airbase':'airbase_city','aircraft':'CI-22','emptyLoadout':True});assert pilot['success'],pilot;time.sleep(2)
results=[]
for maximized in [True,False]:
 layer('mapView',maximized=maximized);time.sleep(1);beforeLayer=wait(shown,90);before=ac('state');assert before['connections']==2 and before['units']>=1000 and beforeLayer['admitted']>=300,(before,beforeLayer)
 rpc('debug.frontlineMapTest',{'action':'profileReset'});ac('probeStart');layer('frameProbeStart');initial=layer('motionStart',mode='rigid',production=True)
 try:
  d=measure('dense-multiplayer-'+('full' if maximized else 'cockpit'),20);moving=layer('contourStats');assert moving['motionFrames']>500 and moving['observedAngleTravel']>360,moving
 finally:
  layer('motionStop');probe=record('dense-frames-'+str(maximized),layer('frameProbeStop'));availability=ac('probeStop');profile=rpc('debug.frontlineMapTest',{'action':'profileState'});rpc('debug.frontlineMapTest',{'action':'profileStop'})
 after=ac('state');afterLayer=layer();a=availability;availabilityPercent=100*a['displayFrames']/(a['displayFrames']+a['hiddenFrames']);frame=d['metrics']['MsBetweenPresents']
 checks=dict(workload=before['connections']==2 and after['connections']==2 and before['units']>=1000 and beforeLayer['admitted']>=300 and not beforeLayer['fixtureActive'],motion=moving['motionFrames']>500 and moving['observedAngleTravel']>360,frames=d['presentRate']>=criteria['livePresentRateMin'],p99=frame['p99']<=criteria['p99FrameMaxMs'],stutters=frame['over50msPercent']<=criteria['over50msPercentMax'],availability=availabilityPercent>=criteria['availabilityMinPercent'],gap=a['longestHiddenSeconds']<=criteria['maximumGapSeconds'])
 results.append(dict(maximized=maximized,success=all(checks.values()),checks=checks,before=before,beforeLayer=beforeLayer,after=after,afterLayer=afterLayer,motionBefore=initial,motionDuring=moving,measurement=d,availability=availability,availabilityPercent=availabilityPercent,readProfile=profile))
 record('dense-multiplayer-result',dict(success=len(results)==2 and all(x['success'] for x in results),binarySha256=EXPECTED,criteria=criteria,cases=results,scope='Actual production fields, two players, recorded unit/contact densities, continuous UI parent motion. Analytic ceiling tests are separate; no larger-session or other hardware guarantee.'));print('Dense UDP moving view',maximized,checks,flush=True)
layer('mapView',maximized=True)

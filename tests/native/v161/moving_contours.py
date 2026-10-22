from native_checks import *
results=[]
ac('pause',scale=1);pilot=rpc('ai.spawnAtAirbaseWithControl',{'airbase':'airbase_city','aircraft':'CI-22','emptyLoadout':True});assert pilot['success'],pilot;record('minimap-pilot-setup',dict(success=True,action=pilot.get('action'),airbase=pilot.get('airbase'),aircraft=pilot.get('aircraft'),scope='Native game-kit spawn into player-controlled aircraft, required for actual cockpit minimap visibility. Not hardware input certification.'));time.sleep(2)
ac('pause',scale=0);select('GroundControl');wait(shown,90)
for maximized in [True,False]:
 layer('mapView',maximized=maximized);time.sleep(1);wait(shown,90)
 for segments in [2048,8192]:
  layer('denseFixtureBegin',segments=segments)
  try:
   time.sleep(1);before=layer('contourStats');layer('motionStart',mode='rigid');time.sleep(12);during=layer('contourStats');check=layer('vectorFixtureCheck');assert check['success'],check;assert during['motionFrames']>200 and during['observedAngleTravel']>180,during
   # Stop may restore the starting transform; rigid restoration must also preserve metric.
   layer('motionStop');time.sleep(.5);after=layer('contourStats');delta=after['builds']-before['builds'];assert delta==0,(before,after)
   zoomBefore=layer('contourStats');layer('motionStart',mode='zoom');time.sleep(8);zoomDuring=layer('contourStats');layer('motionStop');time.sleep(.5);zoomAfter=layer('contourStats');check2=layer('vectorFixtureCheck');assert check2['success'] and zoomAfter['builds']>zoomBefore['builds'],check2
   r=dict(maximized=maximized,segments=segments,rigidBefore=before,rigidDuring=during,rigidAfter=after,rigidRebuilds=delta,rigidWidth=check,zoomBefore=zoomBefore,zoomDuring=zoomDuring,zoomAfter=zoomAfter,zoomWidth=check2,success=True);results.append(r);record('moving-contours',dict(success=len(results)==4,cases=results));print('moving',maximized,segments,'rigid builds',delta,'zoom builds',zoomAfter['builds']-zoomBefore['builds'],flush=True)
  finally:layer('motionStop');layer('vectorFixtureEnd')
  wait(shown,90)
layer('mapView',maximized=True);ac('pause',scale=1)

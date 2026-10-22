from native_checks import *
import runpy
start('Escalation');ac('pause',scale=0)
zoom('vector-projection');layers('main-map-layers');r=record('roles',layer('roleContract'));assert all(x['success'] for x in r['results'])
gameplay('native-gameplay')
for section,key,value,restore in [('Model','Layer','invalid','GroundControl'),('Radii','GroundMeters',0,6000),('Confidence','StaleHalfLifeSeconds',0,120)]:
 before=wait(shown);config(section,key,value);time.sleep(.5);s=layer();assert s['layer']==before['layer'] and s['configurationError'] and s['displayed'];record('invalid-'+key,dict(success=True,state=s));config(section,key,restore);wait(lambda:layer()['configurationError'] is None,20);wait(shown,90)
cycles=[]
for i in range(20):
 rpc('debug.frontlineMapTest',{'action':'close'});time.sleep(.3);s=layer();assert not s['displayed'] and s['ownedObjects']==0 and s['ownedTextures']==0,s
 rpc('debug.frontlineMapTest',{'action':'open'});wait(shown,90);cycles.append(safe());record('lifecycle',dict(success=len(cycles)==20,cycles=cycles))
print('20 lifecycle cycles PASS',flush=True)
cat=ac('missionCatalog');mission=next(x for x in cat['missions'] if 'Terrain_naval' in x['map']);start(mission['name']) if False else None
ac('stop');wait(lambda:ac('state')['scene']=='MainMenu',60);ac('start',mission=mission['name']);wait(ready,100);rpc('control.selectTeam',{'faction':0});rpc('debug.frontlineMapTest',{'action':'open'});ac('pause',scale=0)
r=wait(lambda:(lambda x:x if x['terrain']['ready'] and x.get('terrainCoverageUnavailable') else False)(layer()),90);assert not r['displayed'] and r['ownedTextures']==0,r
record('coverage-warning',dict(success=True,state=r,screenshot=screenshot('naval-coverage-warning')))
config('Model','TerrainEnabled',False);r=wait(shown,90);assert r['friendly']['positive']>0 and not r['terrainCoverageUnavailable'],r
record('explicit-radial-fallback',dict(success=True,scope='Explicit configuration selection uses same setting as GUI fallback; actual desktop click remains unverified',state=r,screenshot=screenshot('naval-radial-explicit')))
config('Model','TerrainEnabled',True);wait(lambda:layer()['terrainCoverageUnavailable'] and not layer()['displayed'],30)
for name in ['AirThreat','Legacy']:
 select(name);safe()
layer('layer',layer='GroundControl');start('Escalation');ac('pause',scale=0)
runpy.run_path(str(HERE/'fault_checks.py'),run_name='__main__')
record('native-feature-final',dict(success=True,state=safe(),layered=layer()));print('Native feature acceptance PASS',flush=True)

from native_support import *

def config(section,key,value):return layer('config',section=section,key=key,value=value)
def select(name):
 layer('layer',layer=name);return wait(lambda:(lambda s:s if s and s['layer']==name else False)(shown()),90)
def safe():
 s=ac('state');assert s['unsafeWhiteImages']==0 and s['overlays']<=1 and s['nativeAnonymousRasterTextures']<=1 and (not s['plugin']['displayed'] or s['shownTextureMatchesPresentation']),s
 return s

def zoom(label):
 rpc('debug.frontlineMapTest',{'action':'zoomOut'});time.sleep(.5);wait(shown,90);record(label+'-begin',layer('vectorFixtureBegin'))
 try:
  time.sleep(.5);a=layer('vectorFixtureCheck');assert a['success'],a
  rpc('debug.frontlineMapTest',{'action':'zoomIn'});time.sleep(.5);b=layer('vectorFixtureCheck');assert b['success'],b
  ac('transform',rotation=30,panX=40,panY=-20);time.sleep(.5);c=layer('vectorFixtureCheck');assert c['success'],c
  ac('resolution',width=1600,height=900);time.sleep(1);d=layer('vectorFixtureCheck');assert d['success'],d
  record(label,dict(success=True,analytic=True,checks=[a,b,c,d]));print(label,'PASS widths',[a['projectedFirstQuadWidthPixels'],b['projectedFirstQuadWidthPixels'],c['projectedFirstQuadWidthPixels'],d['projectedFirstQuadWidthPixels']],flush=True)
 finally:record(label+'-end',layer('vectorFixtureEnd'));ac('resolution',width=1280,height=800);ac('transform',rotation=0,panX=0,panY=0)
 wait(shown,90)

def gameplay(label):
 results=[]
 for action in ['freshnessContract','settingsContract','knowledge','hidden','hiddenDeath','ownership','death','raycast']:
  wait(shown,90);data=ac(action);results.append(dict(case=action,result=data));record(label,dict(cases=results));print(action,data.get('success'),flush=True);assert data['success'],data
  wait(shown,90);safe()

def layers(label, requirePositive=True):
 results=[]
 for name in ['GroundControl','AirThreat','NavalInfluence','Confidence','Legacy']:
  s=select(name);assert s['configurationError'] is None,s
  if requirePositive or name in ['AirThreat','Legacy']:assert s['friendly']['positive']>0,s
  assert not s['graphics'] or all(not g['raycast'] for g in s['graphics'])
  if name!='GroundControl':assert s['contours']==0,s
  results.append(dict(layered=s,native=safe(),projection=wait(audit),metrics=layer('metrics'),screenshot=screenshot(label+'-'+name)))
  record(label,dict(success=len(results)==5,cases=results));print('layer',name,'cells',s['friendly']['positive'],'contours',s['contours'],flush=True)
 select('GroundControl')

if __name__=='__main__':
 start('Escalation');ac('pause',scale=0)
 zoom('vector-zoom-green');layers('main-map-layers');record('roles-green',layer('roleContract'));gameplay('native-gameplay')
 # Invalid layer/radius reload must keep working settings and resource bounds.
 for section,key,value,restore in [('Model','Layer','invalid','GroundControl'),('Radii','GroundMeters',0,6000),('Confidence','StaleHalfLifeSeconds',0,120)]:
  before=wait(shown);config(section,key,value);time.sleep(.5);s=layer();record('invalid-'+key,dict(before=before,after=s));assert s['layer']==before['layer'] and s['configurationError'] and s['displayed'];config(section,key,restore);wait(lambda:layer()['configurationError'] is None,20);wait(shown,90)
 for i in range(5):
  rpc('debug.frontlineMapTest',{'action':'close'});time.sleep(.4);s=layer();assert not s['displayed'] and s['ownedObjects']==0 and s['ownedTextures']==0,s
  rpc('debug.frontlineMapTest',{'action':'open'});wait(shown,90);safe()
 record('hide-reopen',dict(success=True,cycles=5,state=layer()))
 cat=ac('missionCatalog');mission=next(x for x in cat['missions'] if 'Terrain_naval' in x['map']);record('naval-mission',mission);start(mission['name']);ac('pause',scale=0);layers('naval-map-layers',False);record('naval-map-state',layer());assert layer()['terrain']['known']>0 and layer()['terrain']['water']>0
 start('Escalation');ac('pause',scale=1);wait(shown,90);record('post-native-checks',dict(success=True,state=ac('state'),layered=layer()));print('All native checks PASS',flush=True)

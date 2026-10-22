from native_checks import *
ac('pause',scale=0);select('GroundControl')
results=[]
def rec(name,data):
 results.append(dict(case=name,result=data));record('native-faults',dict(cases=results));print(name,'PASS',flush=True)
for stage in ['Prepare','Commit']:
 before=ac('state');ac('fault',stage=stage)
 fired=wait(lambda:(lambda s:s if s['faults']>before['faults'] else False)(ac('state')),40)
 assert not fired['plugin']['displayed'] and fired['plugin']['ownedTextures']==0 and fired['unsafeWhiteImages']==0,fired
 recovered=wait(shown,90);rec(stage,dict(success=True,afterFault=fired,recovered=recovered));safe()
# Partial creation is armed while closed so the actual production Commit constructs the root.
rpc('debug.frontlineMapTest',{'action':'close'});wait(lambda:not layer()['displayed'],20);before=ac('state');ac('partialCreationFault');rpc('debug.frontlineMapTest',{'action':'open'})
fired=wait(lambda:(lambda s:s if s['partialFaults']>before['partialFaults'] else False)(ac('state')),40)
assert fired['unsafeWhiteImages']==0 and not fired['plugin']['displayed'] and fired['plugin']['ownedObjects']==0,fired
rec('partialCreation',dict(success=True,afterFault=fired,recovered=wait(shown,90)));safe()
before=ac('state');ac('dirtyMaterialFault');fired=wait(lambda:(lambda s:s if s['dirtyFaults']>before['dirtyFaults'] else False)(ac('state')),40)
assert fired['unsafeWhiteImages']==0 and fired['overlays']<=1 and (not fired['plugin']['displayed'] or fired['shownTextureMatchesPresentation']),fired
rec('dirtyMaterial',dict(success=True,afterFault=fired,recovered=wait(shown,90)));safe()
before=ac('state');ac('revokeOnPrepare');fired=wait(lambda:(lambda s:s if s['nativeRevocations']>before['nativeRevocations'] else False)(ac('state')),40)
assert fired['plugin']['rejected']>before['plugin']['rejected'] and not fired['plugin']['displayed'] and fired['plugin']['ownedTextures']==0,fired
rec('beforePublishRevocation',dict(success=True,afterReject=fired,recovered=wait(shown,90)));safe()
assert ac('paletteGreen')['success'];wait(shown,90);data=ac('gpuColors');assert data['success'],data;rec('gpuPalette',data)
config('Visual','FriendlyColor','419BF5');config('Visual','TerritoryAlpha',.16);wait(shown,90)
ac('pause',scale=1);record('faults-final',dict(success=True,state=safe(),layered=layer()))

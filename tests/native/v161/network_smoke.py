from guards import validate
validate(client=True,performance=True)
from native_checks import *
CLIENT=Path(os.environ['FLM_CLIENT_ROOT']).resolve()
def nr(action,port=42680,**kw):return rpc('debug.frontlineMapNetwork',dict(action=action,**kw),port=port)
def get(port):return rpc('debug.frontlineMapAcceptance',{'action':'state'},port=port)
def clientReady():
 s=get(42681);return s if s['running'] and s['client'] and not s['server'] and s['playerCount']==2 and s['units']>100 else False
def clientShown():
 s=get(42681);p=s['plugin'];return s if p['displayed'] and p['ownedTextures']==1 and p['ownedObjects']==2 and s['unsafeWhiteImages']==0 and s['shownTextureMatchesPresentation'] else False
ac('stop');wait(lambda:(lambda s:s['scene']=='MainMenu' and s['server'] is False)(ac('state')),60);nr('startHost',mission='Escalation');wait(ready,100);openmap()
args=[PWSH,'-NoProfile','-File',win(HERE/'launch.ps1'),'-GameRoot',win(CLIENT),'-MenuOnly','-LogFile',win(OUT/'final-network-client-player.log')];r=subprocess.run(args,check=True,capture_output=True,text=True);record('final-network-client-launch',json.loads(r.stdout));wait(lambda:(lambda s:s['scene']=='MainMenu' and s['server'] is False and s['client'] is False)(get(42681)),90)
verify_running(CLIENT,42681)
results=[]
try:
 for reconnect in [False,True]:
  if reconnect:
   nr('disconnect',42681);wait(lambda:get(42681)['scene']=='MainMenu' and get(42681)['client'] is False,60)
  nr('connect',42681);wait(clientReady,100)
  for port,faction in [(42680,0),(42681,1)]:rpc('control.selectTeam',{'faction':faction},port=port);rpc('debug.frontlineMapTest',{'action':'open'},port=port)
  wait(shown,90);c=wait(clientShown,90);h=ac('state');ha=wait(audit);ca=rpc('debug.frontlineMapTest',{'action':'audit'},port=42681);assert ha['success'] and ca['success'] and h['viewer']!=c['viewer'] and h['connections']==2,(h,c)
  results.append(dict(reconnect=reconnect,host=h,client=c,hostAudit=ha,clientAudit=ca));record('final-network-smoke',dict(success=len(results)==2,binarySha256=hashlib.sha256((PROFILE/'BepInEx/plugins/FrontlineMap.dll').read_bytes()).hexdigest(),scope='Final exact DLL two-process literal loopback UDP; opposing viewers, current native marker audits, bounded displayed texture/vector resources and disconnect/reconnect. No remote or public network claim.',cases=results));print('UDP',reconnect,'PASS',flush=True)
 import runpy
 runpy.run_path(str(HERE/'dense_multiplayer.py'),run_name='__main__')
finally:stop(42681,CLIENT)
start('Escalation');ac('pause',scale=1);wait(shown,90);record('network-leave',dict(success=True,state=safe(),layered=layer()));print('Offline mission restored',flush=True)

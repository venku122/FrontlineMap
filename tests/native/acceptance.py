import os,subprocess,json,time
from pathlib import Path
HERE=Path(__file__).resolve().parent
ROOT=Path(os.environ.get('FLM_EVIDENCE_ROOT',str(HERE.parents[1]/'artifacts/native-regressions')))
ROOT.mkdir(parents=True,exist_ok=True)
GAME_ROOT=Path(os.environ.get('FLM_GAME_ROOT',str(HERE.parents[2]/'FrontlineMap-runtime-test')))
RPC_SCRIPT=Path(os.environ['FLM_RPC_SCRIPT'])
SCRIPT=subprocess.check_output(['wslpath','-w',str(RPC_SCRIPT)],text=True).strip()
PORT=int(os.environ.get('FLM_PORT','42680'))
def rpc(method,params=None,label=None,port=None):
 a=subprocess.run(['/mnt/c/Program Files/PowerShell/7/pwsh.exe','-NoProfile','-File',SCRIPT,'-HostName',os.environ.get('FLM_BRIDGE_HOST','172.24.192.1'),'-Port',str(port or PORT),'-Method',method,'-ParamsJson',json.dumps(params or {})],capture_output=True,text=True,timeout=40)
 if a.returncode:raise RuntimeError((a.stdout+a.stderr)[-2500:])
 data=json.loads(a.stdout)
 if label:(ROOT/(label+'.json')).write_text(json.dumps(data,indent=2)+'\n')
 return data
def ac(action,**kw):return rpc('debug.frontlineMapAcceptance',dict(action=action,**kw))
def state():return rpc('debug.frontlineMapRuntime')
def wait(fn,timeout=60):
 deadline=time.monotonic()+timeout;last=None
 while time.monotonic()<deadline:
  try:
   last=fn()
   if last:return last
  except Exception as e:last=str(e)
  time.sleep(.3)
 raise RuntimeError('Timed out: '+str(last))
def ready():
 s=ac('state');return s if s['running'] and s['units']>100 else False
def active():
 s=state();image=s.get('overlay') or {};return s if s['displayed'] and s['ownedTextures']==1 and s['ownedObjects']==1 and image.get('enabled') and image.get('texturePresent') else False
def audit():
 a=rpc('debug.frontlineMapTest',{'action':'audit'});return a if a.get('success') else False

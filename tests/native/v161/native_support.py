"""Canonical adaptation of executed native_v160.py; no deployment/bootstrap behavior."""
import os,sys,json,time,subprocess,shutil,hashlib
from pathlib import Path
from guards import profile,validate

HERE=Path(__file__).resolve().parent
REPO=HERE.parents[2]
PROFILE=validate()
OUT=Path(os.environ['FLM_EVIDENCE_ROOT'])
OUT.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(HERE.parent))
import acceptance as transport
PWSH=os.environ.get('FLM_PWSH','/mnt/c/Program Files/PowerShell/7/pwsh.exe')
PORT=int(os.environ.get('FLM_PORT','42680'))
def win(p):return subprocess.check_output(['wslpath','-w',str(p)],text=True).strip()
def record(name,data):(OUT/(name+'.json')).write_text(json.dumps(data,indent=2)+'\n');return data

def verify_running(profile_root,port=PORT):
 profile(profile_root)
 status=transport.rpc('debug.frontlineMapNetwork',{'action':'state'},port=port)
 if status.get('executable','').casefold()!=win(Path(profile_root)/'NuclearOption.exe').casefold():
  raise RuntimeError('Bridge executable does not match explicitly authorized disposable profile')
 return status

rpc=transport.rpc
ac=transport.ac
wait=transport.wait
ready=transport.ready
audit=transport.audit

def layer(action='state',**kw):return rpc('debug.frontlineMapLayered',dict(action=action,**kw))
def shown():
 s=layer();return s if s.get('displayed') and s['ownedTextures']==1 and s['ownedObjects']==(1 if s['layer']=='Legacy' else 2) and len(s['images'])==1 and s['images'][0]['enabled'] and s['images'][0]['texture'] else False
def stop(port=PORT,profile=PROFILE):
 verify_running(profile,port)
 try:rpc('bridge.shutdown',port=port)
 except Exception:pass
 cmd="Get-CimInstance Win32_Process -Filter \"Name='NuclearOption.exe'\" | Where-Object { $_.ExecutablePath -eq '"+win(profile/'NuclearOption.exe').replace("'","''")+"' } | ForEach-Object { Wait-Process -Id $_.ProcessId -Timeout 20 }"
 subprocess.run([PWSH,'-NoProfile','-Command',cmd],check=True,capture_output=True,text=True)
def launch(label,profile=PROFILE,menu=False):
 profile=Path(profile)
 args=[PWSH,'-NoProfile','-File',win(HERE/'launch.ps1'),'-GameRoot',win(profile),'-LogFile',win(OUT/(label+'-player.log'))]
 if menu:args+=['-MenuOnly']
 r=subprocess.run(args,check=True,capture_output=True,text=True);record(label+'-launch',json.loads(r.stdout));return wait(lambda:(lambda s:s.get('scene')=='MainMenu' and s.get('server') is False)(ac('state')),90) if menu else wait(ready,100)
def openmap():rpc('control.selectTeam',{'faction':0});rpc('debug.frontlineMapTest',{'action':'open'});return wait(shown,90)
def screenshot(label):
 data=rpc('artifact.captureScreenshot',{'label':label});path=Path(subprocess.check_output(['wslpath','-u',data['path']],text=True).strip());wait(lambda:path.is_file(),20);time.sleep(.5);dst=OUT/(label+'.png');shutil.copy2(path,dst);return record(label+'-screenshot',dict(native=data,artifact=dst.name,sha256=hashlib.sha256(dst.read_bytes()).hexdigest()))
def start(mission):
 ac('stop');wait(lambda:ac('state')['scene']=='MainMenu',60);ac('start',mission=mission);wait(ready,100);return openmap()

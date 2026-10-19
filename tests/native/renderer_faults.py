from acceptance import *
import argparse,hashlib
p=argparse.ArgumentParser();p.add_argument('--label',required=True);p.add_argument('--case',choices=['partial','dirty'],default='partial');args=p.parse_args()
profile=GAME_ROOT
def win(p):return subprocess.check_output(['wslpath','-w',str(p)],text=True).strip()
(ROOT/(args.label+'-launch.json')).write_text(subprocess.check_output(['/mnt/c/Program Files/PowerShell/7/pwsh.exe','-NoProfile','-File',win(HERE/'launch-local.ps1'),'-GameRoot',win(profile),'-LogFile',win(ROOT/(args.label+'-player.log'))],text=True))
result={'case':args.case,'binary_sha256':hashlib.sha256((profile/'BepInEx/plugins/FrontlineMap.dll').read_bytes()).hexdigest()}
try:
 wait(ready,100);rpc('control.selectTeam',{'faction':0});rpc('debug.frontlineMapTest',{'action':'open'});wait(active);ac('pause',scale=0)
 if args.case=='partial':
  rpc('debug.frontlineMapTest',{'action':'close'});wait(lambda:(lambda s:s if s['ownedObjects']==0 else False)(state()));before=ac('state');arm=ac('partialCreationFault');assert arm['armed'];rpc('debug.frontlineMapTest',{'action':'open'});key='partialFaults'
 else:before=ac('state');arm=ac('dirtyMaterialFault');key='dirtyFaults'
 def fired():
  s=ac('state');return s if s[key]>before[key] else False
 first=wait(fired,40);result['afterFault']={k:first[k] for k in ['partialFaults','dirtyFaults','unsafeWhiteImages','overlays','shownTextureMatchesPresentation']};result['afterFault']['plugin']={k:first['plugin'][k] for k in ['displayed','ownedTextures','ownedObjects','published','rejected']}
 rpc('artifact.captureScreenshot',{'label':args.label+'-after-fault'},args.label+'-screenshot')
 def published_after_fault():
  s=ac('state');v=s['plugin'];o=v.get('overlay',{})
  return s if (v['published']>first['plugin']['published'] and v['displayed'] and v['ownedTextures']==1 and v['ownedObjects']==1 and o.get('enabled') and o.get('texturePresent') and s['shownTextureMatchesPresentation'] and s['unsafeWhiteImages']==0) else False
 try:
  recovered=wait(published_after_fault,25);result['recovered']=True;result['subsequentPublicationDelta']=recovered['plugin']['published']-first['plugin']['published'];result['recoveryState']=recovered
 except Exception:result['recovered']=False
 result['success']=first['unsafeWhiteImages']==0 and result['recovered'] and (args.case!='dirty' or first['shownTextureMatchesPresentation'])
 (ROOT/(args.label+'.json')).write_text(json.dumps(result,indent=2));print(json.dumps(result),flush=True)
 assert result['success'],'Renderer fault left a white presentation or failed to recover'
finally:
 (ROOT/(args.label+'-bepinex.log')).write_bytes((profile/'BepInEx/LogOutput.log').read_bytes());rpc('bridge.shutdown');cmd="Get-CimInstance Win32_Process -Filter \"Name='NuclearOption.exe'\" | Where-Object { $_.ExecutablePath -eq '"+win(profile/'NuclearOption.exe')+"' } | ForEach-Object { Wait-Process -Id $_.ProcessId -Timeout 20 }";subprocess.run(['/mnt/c/Program Files/PowerShell/7/pwsh.exe','-NoProfile','-Command',cmd],check=True,capture_output=True,text=True)

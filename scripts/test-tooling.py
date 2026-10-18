"""Executes PowerShell against disposable filesystem fixtures; never launches the game."""
import hashlib,json,pathlib,shutil,subprocess,sys,os
root=pathlib.Path(__file__).resolve().parent.parent
out=root/'artifacts'/'tooling-tests'
if out.exists(): shutil.rmtree(out)
out.mkdir(parents=True)
ps=os.environ.get('FRONTLINEMAP_POWERSHELL') or shutil.which('powershell.exe') or shutil.which('pwsh') or shutil.which('powershell')
if not ps: raise SystemExit('PowerShell unavailable')
def host(p):
 s=str(p)
 if ps.endswith('.exe') and s.startswith('/mnt/c/'): return 'C:/'+s[len('/mnt/c/'):]
 return s
results=[]
def run(case,script,*args,expected=0):
 cmd=[ps,'-NoProfile','-File',host(root/'scripts'/script),*map(str,args)]
 p=subprocess.run(cmd,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
 text=p.stdout.decode('utf-8',errors='replace')
 (out/(case+'.log')).write_text(text)
 assert (p.returncode==0)==(expected==0),(case,p.returncode,text)
 results.append(dict(case=case,status='pass',exit_code=p.returncode,log=case+'.log'))
 return text
fake=out/'non default Steam path';managed=fake/'NuclearOption_Data'/'Managed';managed.mkdir(parents=True)
run('missing-reference','build.ps1','-GameDir',host(fake),'-PreflightOnly',expected=1)
for name in ['Assembly-CSharp','UnityEngine','UnityEngine.CoreModule','UnityEngine.UI','UnityEngine.InputLegacyModule','UnityEngine.IMGUIModule','UnityEngine.TextRenderingModule','UnityEngine.UIModule','Mirage']:(managed/(name+'.dll')).write_bytes(b'preflight-only-file-fixture')
loader=fake/'BepInEx'/'core';loader.mkdir(parents=True)
run('missing-loader','build.ps1','-GameDir',host(fake),'-PreflightOnly',expected=1)
(loader/'BepInEx.dll').write_bytes(b'preflight-only-file-fixture')
run('nondefault-path','build.ps1','-GameDir',host(fake),'-PreflightOnly')
package=out/'package'
run('package','package.ps1','-OutputDir',host(package))
profile=out/'disposable profile';(profile/'BepInEx'/'core').mkdir(parents=True)
(profile/'BepInEx'/'core'/'BepInEx.dll').write_bytes(b'not-a-real-loader-test-fixture')
plugin=profile/'BepInEx'/'plugins'/'FrontlineMap.dll';plugin.parent.mkdir(parents=True);plugin.write_bytes(b'original-plugin')
config=profile/'BepInEx'/'config'/'com.noms.frontlinemap.cfg';config.parent.mkdir(parents=True);config.write_text('original-config')
unrelated=plugin.parent/'unrelated.txt';unrelated.write_text('preserve')
def tree():return {str(p.relative_to(profile)):hashlib.sha256(p.read_bytes()).hexdigest() for p in profile.rglob('*') if p.is_file()}
before=tree()
expected_hash=hashlib.sha256(plugin.read_bytes()).hexdigest()
run('deploy-whatif','deploy.ps1','-ProfileDir',host(profile),'-PackageDir',host(package),'-ExpectedInstalledSha256',expected_hash,'-WhatIf')
assert before==tree(),'WhatIf mutated profile'
run('deploy-guard','deploy.ps1','-ProfileDir',host(profile),'-PackageDir',host(package),'-ExpectedInstalledSha256',expected_hash,expected=1)
assert before==tree(),'guard mutated profile'
run('deploy-wrong-hash','deploy.ps1','-ProfileDir',host(profile),'-PackageDir',host(package),'-ExpectedInstalledSha256','0'*64,'-AuthorizedDisposableProfile',expected=1)
assert before==tree(),'hash mismatch mutated profile'
run('deploy-unexpected-existing','deploy.ps1','-ProfileDir',host(profile),'-PackageDir',host(package),'-ExpectAbsent','-AuthorizedDisposableProfile',expected=1)
assert before==tree(),'unexpected existing file mutated profile'
run('deploy','deploy.ps1','-ProfileDir',host(profile),'-PackageDir',host(package),'-ExpectedInstalledSha256',expected_hash,'-AuthorizedDisposableProfile')
assert plugin.read_bytes()==(package/'FrontlineMap.dll').read_bytes()
manifest=next(profile.glob('FrontlineMap-rollback-*/rollback.json'))
config.write_text('candidate-generated-change')
run('rollback','rollback.ps1','-ProfileDir',host(profile),'-Manifest',host(manifest),'-AuthorizedDisposableProfile')
assert plugin.read_bytes()==b'original-plugin';assert config.read_text()=='original-config';assert unrelated.read_text()=='preserve'
run('target-mode-blocked','validate.ps1','-Mode','Target',expected=2)
# A same-version altered DLL must not be attributed to the attested build.
binary=root/'bin'/'Release'/'net472'/'FrontlineMap.dll';original=binary.read_bytes()
try:
 binary.write_bytes(original+b'stale-same-version-fixture')
 run('package-stale-binary','package.ps1','-OutputDir',host(out/'stale-package'),expected=1)
finally:binary.write_bytes(original)
# Test a copied source tree so the working implementation is never changed by this fixture.
fixture=out/'source fixture';(fixture/'scripts').mkdir(parents=True)
shutil.copytree(root/'src',fixture/'src')
for name in ['Plugin.cs','FrontlineMap.csproj','LICENSE','README.md']:shutil.copy2(root/name,fixture/name)
for name in ['package.ps1','source-state.ps1']:shutil.copy2(root/'scripts'/name,fixture/'scripts'/name)
(fixture/'bin/Release/net472').mkdir(parents=True);shutil.copy2(binary,fixture/'bin/Release/net472/FrontlineMap.dll')
(fixture/'artifacts/build').mkdir(parents=True)
for name in ['binary.json','references.json']:shutil.copy2(root/'artifacts/build'/name,fixture/'artifacts/build'/name)
def fixture_package(case,expected):
 cmd=[ps,'-NoProfile','-File',host(fixture/'scripts/package.ps1'),'-OutputDir',host(out/case)]
 result=subprocess.run(cmd,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
 (out/(case+'.log')).write_text(result.stdout.decode(errors='replace'))
 assert (result.returncode==0)==(expected==0),(case,result.stdout)
 results.append(dict(case=case,status='pass',exit_code=result.returncode,log=case+'.log'))
fixture_package('package-attested-source',0)
with (fixture/'Plugin.cs').open('a') as f:f.write('\n// source changed after the attested build\n')
fixture_package('package-stale-source',1)
(out/'results.json').write_text(json.dumps(dict(harness='PowerShell filesystem fixtures; no target deployment or game load',cases=results),indent=2))
print(f'{len(results)} PowerShell tooling cases passed; disposable fixture rollback restored original hashes.')

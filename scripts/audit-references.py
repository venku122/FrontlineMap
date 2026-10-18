"""Verify MSBuild resolved each explicit game/loader DLL from the selected root, not stale candidates."""
import argparse,json,pathlib,subprocess
p=argparse.ArgumentParser();p.add_argument('--dotnet',default='dotnet');p.add_argument('--managed-dir',required=True);p.add_argument('--loader-dir',required=True);p.add_argument('--output',required=True);a=p.parse_args()
root=pathlib.Path(__file__).resolve().parent.parent
cmd=[a.dotnet,'msbuild',str(root/'FrontlineMap.csproj'),'-t:ResolveReferences','-p:Configuration=Release','-p:ManagedDir='+str(pathlib.Path(a.managed_dir).resolve()),'-p:LoaderDir='+str(pathlib.Path(a.loader_dir).resolve()),'-getItem:ReferencePath']
r=subprocess.run(cmd,stdout=subprocess.PIPE,stderr=subprocess.STDOUT);text=r.stdout.decode(errors='replace')
if r.returncode:raise SystemExit(text)
obj=json.loads(text);items=obj['Items']['ReferencePath'];resolved={pathlib.Path(v['Identity']).stem:v for v in items}
expected={n:pathlib.Path(a.managed_dir)/f'{n}.dll' for n in ['Assembly-CSharp','UnityEngine','UnityEngine.CoreModule','UnityEngine.UI','UnityEngine.InputLegacyModule','UnityEngine.IMGUIModule','UnityEngine.TextRenderingModule','UnityEngine.UIModule','Mirage']};expected['BepInEx']=pathlib.Path(a.loader_dir)/'BepInEx.dll'
records=[]
for name,path in expected.items():
 actual=resolved.get(name);assert actual,(name,'unresolved')
 assert pathlib.Path(actual['Identity']).resolve()==path.resolve(),(name,'resolved unrelated file',actual['Identity'])
 assert actual['CopyLocal'].lower()=='false',(name,'must not distribute game DLL')
 records.append(dict(reference=name,status='pass',selected_sha256=__import__('hashlib').sha256(path.read_bytes()).hexdigest(),resolved_from=actual['ResolvedFrom'],copy_local=False))
pathlib.Path(a.output).write_text(json.dumps(dict(status='pass',cases=records,shadow_fixture_present=(root/'artifacts/tooling-tests/non default Steam path').exists()),indent=2));print(f'{len(records)} references resolve only from intended roots and are non-copying.')

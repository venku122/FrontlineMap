from native_checks import *
root=PROFILE/'BepInEx/FrontlineMap-replay';before=set(root.rglob('*.snapshot.json')) if root.exists() else set()
ac('pause',scale=0);select('GroundControl')
try:
 config('Diagnostics','ExportSnapshots',True)
 def exported():
  for f in root.rglob('*.snapshot.json'):
   if f not in before:
    try:
     d=json.loads(f.read_text())
     if 'terrain' in d:return f,d
    except (ValueError,OSError):pass
  return False
 f,data=wait(exported,40)
 assert data['schema']==2 and data['model']['terrain_enabled'] and data['terrain']['columns']==128 and data['terrain']['rows']==128 and len(data['terrain']['known'])==16384 and all(data['terrain']['known']),data.keys()
 command=[os.environ.get('FLM_DOTNET','dotnet'),'run','--project',str(REPO/'tools/FrontlineMap.Replay/FrontlineMap.Replay.csproj'),'-c','Release','--no-build','--','--fixtures',str(f),'--output',str(OUT/'production-layered-replay'),'--compare','oracle,candidate','--layered','true']
 r=subprocess.run(command,capture_output=True,text=True,timeout=180);(OUT/'production-replay.log').write_text(r.stdout+r.stderr);assert r.returncode==0,r.stdout+r.stderr
 replay=json.loads((OUT/'production-layered-replay/comparison.json').read_text());assert replay['status']=='pass',replay
 record('production-export-replay',dict(success=True,binarySha256=hashlib.sha256((PROFILE/'BepInEx/plugins/FrontlineMap.dll').read_bytes()).hexdigest(),schema=data['schema'],layer=data['model']['layer'],stamps=len(data['stamps']),terrainColumns=data['terrain']['columns'],terrainRows=data['terrain']['rows'],known=sum(data['terrain']['known']),sourceSnapshotSha256=hashlib.sha256(f.read_bytes()).hexdigest(),replay=replay,scope='Actual production Mono export includes cached terrain; independent raw-terrain oracle compared to production incremental scheduler in .NET tool. Raw permitted observation positions stay in private disposable profile. This is not a byte comparison of native Unity FieldResult to tool buffers.'))
 print('Production terrain export replay PASS',flush=True)
finally:config('Diagnostics','ExportSnapshots',False);wait(shown,90);ac('pause',scale=1)

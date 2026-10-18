"""Checks real regression assertions by temporarily mutating owned production source, then restoring it."""
import os,pathlib,subprocess,json
root=pathlib.Path(__file__).resolve().parent.parent
sdk=os.environ.get('FRONTLINEMAP_DOTNET','dotnet')
out=root/'artifacts'/'mutation';out.mkdir(parents=True,exist_ok=True)
cases=[('retain-opposing-support','src/Core/ScalarField.cs','return field;','Array.Clear(field.Enemy,0,field.Enemy.Length); return field;','EqualOpponentsRetainSupport'),('reject-unresolved-viewer','src/Core/ObservationPolicy.cs','if (viewer != ViewerMode.Assigned)','if (false)','UnresolvedAndSpectatorFailClosed'),('publication-context','src/Runtime/OverlayController.cs','valid && context.SameContext(result.Snapshot.Identity) &&','true &&','ContextChangeAfterPrepareRejectsStaging'),('expire-active-work','src/Runtime/StepScheduler.cs','DiscardExpired(clock());','','ExpiredActiveReleasesBudgetForFreshPending'),('visible-alpha','src/Core/FieldClassifier.cs','byte alpha = (byte)Math.Round(255 * settings.Opacity);','byte alpha = 0;','ByteAlphaSurvivesSingleAirbase')]
results=[]
for name,file,old,new,filter in cases:
 p=root/file;original=p.read_text();assert old in original,(name,'mutation target missing')
 try:
  p.write_text(original.replace(old,new,1))
  result=subprocess.run([sdk,'test',str(root/'tests/FrontlineMap.Core.Tests'),'-c','Release','--filter',filter],stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
  text=result.stdout.decode(errors='replace');(out/(name+'.log')).write_text(text)
  assert result.returncode!=0 and 'Failed!' in text and 'error CS' not in text,(name,'must fail an assertion, not compilation',text)
  results.append(dict(case=name,mutant='rejected_by_regression_assertion',exit_code=result.returncode))
 finally:p.write_text(original)
(out/'results.json').write_text(json.dumps(results,indent=2));print(f'{len(results)} deliberate production mutations rejected; original files restored.')

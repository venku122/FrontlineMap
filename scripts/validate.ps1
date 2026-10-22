[CmdletBinding()]
param(
 [string]$GameDir,
 [ValidateSet('Core','Native','Performance','Readiness','Target')][string]$Mode='Core',
 [string]$Dotnet='dotnet',
 [string]$Python='python',
 [string]$EvidenceDir,
 [string]$Binary,
 [ValidateRange(0.01,48)][double]$MaxEvidenceAgeHours=48
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'source-state.ps1')
$folder=Join-Path $root 'artifacts/validation'
New-Item -ItemType Directory -Force $folder | Out-Null
$coreResult=$null
if($Mode -in @('Core','Readiness','Target')){
 $sourceBefore=(Get-SourceFingerprint $root).tree_sha256
 # Remove the prior result before execution so failed runs cannot leave a current pass.
 $coreFile=Join-Path $folder 'core.json'
 Remove-Item -LiteralPath $coreFile -ErrorAction SilentlyContinue
 & $Python (Join-Path $PSScriptRoot 'test-readiness.py')
 if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
 & $Dotnet test (Join-Path $root 'tests/FrontlineMap.Core.Tests/FrontlineMap.Core.Tests.csproj') -c Release --logger 'trx;LogFileName=core-tests.trx' --results-directory (Join-Path $root 'artifacts/tests')
 if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
 & $Dotnet run --project (Join-Path $root 'tools/FrontlineMap.Replay/FrontlineMap.Replay.csproj') -c Release -- --fixtures (Join-Path $root 'tests/fixtures') --compare oracle,candidate --output (Join-Path $root 'artifacts/replay')
 if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
 & $Dotnet run --project (Join-Path $root 'tools/FrontlineMap.Replay/FrontlineMap.Replay.csproj') -c Release -- --fixtures (Join-Path $root 'tests/fixtures/layered') --compare oracle,candidate --layered true --output (Join-Path $root 'artifacts/replay-layered')
 if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
 if((Get-SourceFingerprint $root).tree_sha256 -ne $sourceBefore){throw 'Source changed during Core validation; repeat Core against stable source.'}
 $coreResult=@{gate='core';status='pass';captured_at_utc=[DateTime]::UtcNow.ToString('o');source_tree_sha256=$sourceBefore;scope='Portable model tests, readiness fixtures and synthetic replay; no Unity load or runtime performance claim';trx_sha256=(Get-FileHash (Join-Path $root 'artifacts/tests/core-tests.trx')).Hash;replay_sha256=(Get-FileHash (Join-Path $root 'artifacts/replay/comparison.json')).Hash;layered_replay_sha256=(Get-FileHash (Join-Path $root 'artifacts/replay-layered/comparison.json')).Hash}
 $coreResult | ConvertTo-Json -Depth 5 | Set-Content $coreFile
 if($Mode -eq 'Core'){exit 0}
}
$gate=switch($Mode){'Native'{'native'}'Performance'{'performance'}default{'target'}}
$runtimeFile=Join-Path $folder ($gate+'-runtime.json')
$arguments=@((Join-Path $PSScriptRoot 'readiness.py'),'--root',$root,'--gate',$gate,'--max-age-hours',$MaxEvidenceAgeHours.ToString([Globalization.CultureInfo]::InvariantCulture),'--output',$runtimeFile)
if($EvidenceDir){$arguments+=@('--evidence-dir',$EvidenceDir)}
if($Binary){$arguments+=@('--binary',$Binary)}
& $Python @arguments
$runtimeExit=$LASTEXITCODE
if($Mode -in @('Readiness','Target')){
 $runtime=Get-Content -Raw $runtimeFile | ConvertFrom-Json
 $status=if($runtimeExit -eq 0){'pass'}else{'blocked'}
 @{gate='readiness';status=$status;core=$coreResult;runtime=$runtime;scope='Attested target and scenario coverage only; does not certify other hardware or resolutions'} | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $folder 'readiness.json')
}
exit $runtimeExit

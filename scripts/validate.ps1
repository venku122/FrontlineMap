[CmdletBinding()]
param([string]$GameDir,[ValidateSet('Core','Target')][string]$Mode='Core',[string]$Dotnet='dotnet')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent
if($Mode -eq 'Target'){
 $folder=Join-Path $root 'artifacts/validation';New-Item -ItemType Directory -Force $folder | Out-Null
 @{status='blocked';reason='Interactive authorized disposable game profile and observed scenario evidence required';build_is_not_load=$true;required=@('native policy host/client/stale/lost','map landmarks/zoom/masks/DPI','20 lifecycle cycles','multiplayer coexistence','paired performance/freshness trials')} | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $folder 'target-required.json')
 Write-Error 'Target runtime validation is blocked; see artifacts/validation/target-required.json' -ErrorAction Continue
 exit 2
}
& $Dotnet test (Join-Path $root 'tests/FrontlineMap.Core.Tests/FrontlineMap.Core.Tests.csproj') -c Release --logger 'trx;LogFileName=core-tests.trx' --results-directory (Join-Path $root 'artifacts/tests')
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& $Dotnet run --project (Join-Path $root 'tools/FrontlineMap.Replay/FrontlineMap.Replay.csproj') -c Release -- --fixtures (Join-Path $root 'tests/fixtures') --compare oracle,candidate --output (Join-Path $root 'artifacts/replay')
exit $LASTEXITCODE

[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release',[Parameter(Mandatory=$true)][string]$OutputDir)
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'source-state.ps1')
$dll=Join-Path $root "bin/$Configuration/net472/FrontlineMap.dll"
if(-not(Test-Path $dll)){throw 'Build a real candidate DLL before packaging.'}
$attestation=Get-Content (Join-Path $root 'artifacts/build/binary.json') -Raw | ConvertFrom-Json
if((Get-SourceFingerprint $root).tree_sha256 -ne $attestation.source_tree_sha256){throw 'Source differs from build attestation; rebuild with build.ps1.'}
if((Get-FileHash $dll).Hash -ne $attestation.sha256){throw 'DLL differs from build attestation; rebuild.'}
if((Get-FileHash (Join-Path $root 'artifacts/build/references.json')).Hash -ne $attestation.reference_manifest_sha256){throw 'Reference manifest differs from build attestation.'}
$sourceHead=(& git -C $root rev-parse HEAD 2>$null);if($LASTEXITCODE -ne 0){$sourceHead='not_available'}
if($sourceHead -ne $attestation.source_head){throw 'Source HEAD differs from build attestation; rebuild.'}
[xml]$project=Get-Content (Join-Path $root 'FrontlineMap.csproj')
$version=($project.Project.PropertyGroup | Where-Object {$_.Version} | Select-Object -First 1).Version
if([Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString() -ne "$version.0"){throw 'Assembly/project metadata mismatch.'}
if(Test-Path $OutputDir){if(@(Get-ChildItem $OutputDir -Force).Count -gt 0){throw 'Output directory must be empty.'}}else{New-Item -ItemType Directory $OutputDir | Out-Null}
Copy-Item $dll (Join-Path $OutputDir 'FrontlineMap.dll')
foreach($name in @('LICENSE','README.md')){Copy-Item (Join-Path $root $name) $OutputDir}
$hash=(Get-FileHash $dll -Algorithm SHA256).Hash
@{readiness='development_only';version=$version;dll_sha256=$hash;target_build='not_verified_by_packager';source_inspected_baseline_build='24724372';target_load='not_run';performance='not_measured';source_head=$sourceHead;source_tree_sha256=$attestation.source_tree_sha256;reference_manifest_sha256=$attestation.reference_manifest_sha256;source_dirty=[bool](& git -C $root status --porcelain)} | ConvertTo-Json | Set-Content (Join-Path $OutputDir 'package.json')
Get-ChildItem $OutputDir -File | ForEach-Object {"$((Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower())  $($_.Name)"} | Set-Content (Join-Path $OutputDir 'manifest.sha256')
Write-Output "Development candidate packaged at $OutputDir"

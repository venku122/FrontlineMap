[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release',[Parameter(Mandatory=$true)][string]$OutputDir)
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent
$dll=Join-Path $root "bin/$Configuration/net472/FrontlineMap.dll"
if(-not(Test-Path $dll)){throw 'Build a real candidate DLL before packaging.'}
[xml]$project=Get-Content (Join-Path $root 'FrontlineMap.csproj')
$version=($project.Project.PropertyGroup | Where-Object {$_.Version} | Select-Object -First 1).Version
if([Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString() -ne "$version.0"){throw 'Assembly/project metadata mismatch.'}
if(Test-Path $OutputDir){if(@(Get-ChildItem $OutputDir -Force).Count -gt 0){throw 'Output directory must be empty.'}}else{New-Item -ItemType Directory $OutputDir | Out-Null}
Copy-Item $dll (Join-Path $OutputDir 'FrontlineMap.dll')
foreach($name in @('LICENSE','README.md')){Copy-Item (Join-Path $root $name) $OutputDir}
$hash=(Get-FileHash $dll -Algorithm SHA256).Hash
@{readiness='development_only';version=$version;dll_sha256=$hash;target_build='24724372';target_load='not_run';performance='not_measured';source_head=(& git -C $root rev-parse HEAD);source_dirty=[bool](& git -C $root status --porcelain)} | ConvertTo-Json | Set-Content (Join-Path $OutputDir 'package.json')
Get-ChildItem $OutputDir -File | ForEach-Object {"$((Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower())  $($_.Name)"} | Set-Content (Join-Path $OutputDir 'manifest.sha256')
Write-Output "Development candidate packaged at $OutputDir"

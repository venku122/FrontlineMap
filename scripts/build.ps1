[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GameDir,[string]$LoaderDir,[string]$ManagedDir,[ValidateSet('Debug','Release')][string]$Configuration='Release',[string]$Dotnet='dotnet',[switch]$PreflightOnly)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$root=Split-Path $PSScriptRoot -Parent
if(-not $ManagedDir){$ManagedDir=Join-Path $GameDir 'NuclearOption_Data/Managed'}
if(-not $LoaderDir){$LoaderDir=Join-Path $GameDir 'BepInEx/core'}
$managed=@('Assembly-CSharp','UnityEngine','UnityEngine.CoreModule','UnityEngine.UI','UnityEngine.InputLegacyModule','UnityEngine.IMGUIModule','UnityEngine.TextRenderingModule','UnityEngine.UIModule','Mirage')
$hashes=@{}
foreach($name in $managed){$file=Join-Path $ManagedDir "$name.dll";if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw "Missing game reference: $file"};$hashes["Managed/$name.dll"]=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash}
$loader=Join-Path $LoaderDir 'BepInEx.dll'
if(-not(Test-Path -LiteralPath $loader -PathType Leaf)){throw "Missing loader reference: $loader"}
$hashes['Loader/BepInEx.dll']=(Get-FileHash -LiteralPath $loader -Algorithm SHA256).Hash
$artifacts=Join-Path $root 'artifacts/build';New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
@{kind='reference_preflight';configuration=$Configuration;hashes=$hashes;target_load='not_run'} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $artifacts 'references.json')
if($PreflightOnly){Write-Output 'Reference preflight passed (files only; no compile/load).';return}
& $Dotnet build (Join-Path $root 'FrontlineMap.csproj') -c $Configuration "-p:ManagedDir=$ManagedDir" "-p:LoaderDir=$LoaderDir" 2>&1 | Tee-Object (Join-Path $artifacts 'build.log')
if($LASTEXITCODE -ne 0){throw "dotnet build failed: $LASTEXITCODE"}
$dll=Join-Path $root "bin/$Configuration/net472/FrontlineMap.dll"
@{kind='target_reference_build';sha256=(Get-FileHash $dll).Hash;assembly_version=[Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString();target_load='not_run';readiness='development_only'} | ConvertTo-Json | Set-Content (Join-Path $artifacts 'binary.json')

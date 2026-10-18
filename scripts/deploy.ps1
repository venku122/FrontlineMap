[CmdletBinding(SupportsShouldProcess=$true)]
param([Parameter(Mandatory=$true)][string]$ProfileDir,[Parameter(Mandatory=$true)][string]$PackageDir,[switch]$AuthorizedDisposableProfile)
$ErrorActionPreference='Stop'
$profile=[IO.Path]::GetFullPath($ProfileDir)
$source=Join-Path $PackageDir 'FrontlineMap.dll';$metadata=Get-Content (Join-Path $PackageDir 'package.json') -Raw | ConvertFrom-Json
if((Get-FileHash $source).Hash -ne $metadata.dll_sha256){throw 'Package fingerprint mismatch.'}
if(-not(Test-Path (Join-Path $profile 'BepInEx/core/BepInEx.dll'))){throw 'Profile must already contain its authorized loader; this script does not install loaders.'}
$plugins=Join-Path $profile 'BepInEx/plugins'
$duplicates=@(Get-ChildItem $plugins -Filter '*Frontline*.dll' -Recurse -ErrorAction SilentlyContinue | Where-Object {$_.FullName -ne (Join-Path $plugins 'FrontlineMap.dll')})
if($duplicates.Count -gt 0){throw 'Other Frontline DLLs found. Resolve duplicates manually; nothing was changed.'}
if(-not $WhatIfPreference -and -not $AuthorizedDisposableProfile){throw 'Actual deployment requires -AuthorizedDisposableProfile; normal profile is outside this task authority.'}
if($PSCmdlet.ShouldProcess($profile,'Back up tracked files and install FrontlineMap development candidate')){
 $backup=Join-Path $profile ('FrontlineMap-rollback-'+[Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory $backup | Out-Null
 $records=@();$index=0
 foreach($relative in @('BepInEx/plugins/FrontlineMap.dll','BepInEx/config/com.noms.frontlinemap.cfg','BepInEx/config/com.yuulf.frontlinemap.cfg')){
  $file=Join-Path $profile $relative;$exists=Test-Path $file;$copy="$index.backup";$hash=$null
  if($exists){Copy-Item -LiteralPath $file (Join-Path $backup $copy);$hash=(Get-FileHash $file).Hash}
  $records+=@{relative=$relative;existed=[bool]$exists;backup=$copy;sha256=$hash};$index++
 }
 @{profile=$profile;candidate_sha256=$metadata.dll_sha256;files=$records} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $backup 'rollback.json')
 New-Item -ItemType Directory $plugins -Force | Out-Null
 Copy-Item $source (Join-Path $plugins 'FrontlineMap.dll') -Force
 Write-Output (Join-Path $backup 'rollback.json')
}

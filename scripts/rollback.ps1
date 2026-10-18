[CmdletBinding(SupportsShouldProcess=$true)]
param([Parameter(Mandatory=$true)][string]$ProfileDir,[Parameter(Mandatory=$true)][string]$Manifest,[switch]$AuthorizedDisposableProfile)
$ErrorActionPreference='Stop'
$profile=[IO.Path]::GetFullPath($ProfileDir);$data=Get-Content -LiteralPath $Manifest -Raw | ConvertFrom-Json
if($profile -ne $data.profile){throw 'Profile differs from rollback manifest.'}
if(-not $WhatIfPreference -and -not $AuthorizedDisposableProfile){throw 'Rollback requires -AuthorizedDisposableProfile.'}
$backup=Split-Path ([IO.Path]::GetFullPath($Manifest)) -Parent
$allowed=@('BepInEx/plugins/FrontlineMap.dll','BepInEx/config/com.noms.frontlinemap.cfg','BepInEx/config/com.yuulf.frontlinemap.cfg')
if(@($data.files).Count -ne 3){throw 'Invalid rollback file list.'}
$seen=@{}
foreach($record in $data.files){if($record.relative -notin $allowed -or $seen.ContainsKey($record.relative)){throw 'Invalid rollback path.'};$seen[$record.relative]=$true
 if($record.backup -notmatch '^[0-2]\.backup$'){throw 'Invalid backup path.'}
 if($record.existed -and (Get-FileHash (Join-Path $backup $record.backup)).Hash -ne $record.sha256){throw 'Backup fingerprint mismatch.'}
}
$installed=Join-Path $profile $allowed[0]
if((Test-Path $installed) -and (Get-FileHash $installed).Hash -ne $data.candidate_sha256){throw 'Installed DLL changed since deployment; inspect manually before rollback.'}
if($PSCmdlet.ShouldProcess($profile,'Restore exactly the recorded FrontlineMap files')){
 foreach($record in $data.files){$dest=Join-Path $profile $record.relative
  if($record.existed){New-Item -ItemType Directory (Split-Path $dest -Parent) -Force | Out-Null;Copy-Item (Join-Path $backup $record.backup) $dest -Force;if((Get-FileHash $dest).Hash -ne $record.sha256){throw 'Restored fingerprint mismatch.'}}
  elseif(Test-Path -LiteralPath $dest){Remove-Item -LiteralPath $dest}
 }
 Write-Output 'Tracked FrontlineMap files restored; unrelated files untouched.'
}

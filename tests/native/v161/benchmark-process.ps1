[CmdletBinding()]
param(
 [ValidateSet('inspect','stop','measure')][string]$Action,
 [Parameter(Mandatory=$true)][string]$ExpectedExe,
 [int]$GameProcessId,
 [int]$DurationSeconds=60,
 [string]$PresentMon,
 [string]$CsvPath,
 [string]$ResultPath,
 [string]$SessionName
)
$ErrorActionPreference='Stop'
$ExpectedExe=(Resolve-Path -LiteralPath $ExpectedExe).Path
$gameRoot=Split-Path -Parent $ExpectedExe
if (!(Test-Path -LiteralPath (Join-Path $gameRoot '.frontlinemap-disposable') -PathType Leaf)) { throw 'Existing disposable-profile marker required before process inspection or capture.' }
if ((Split-Path -Leaf $ExpectedExe) -ne 'NuclearOption.exe') { throw 'Expected NuclearOption.exe in the marked disposable copy.' }

function Own-Process {
 $p=Get-Process -Id $GameProcessId -ErrorAction Stop
 if (![string]::Equals($p.Path,$ExpectedExe,[StringComparison]::OrdinalIgnoreCase)) { throw 'PID executable guard failed' }
 return $p
}
if ($Action -eq 'inspect') {
 $items=@(Get-CimInstance Win32_Process -Filter "Name='NuclearOption.exe'" | Select-Object ProcessId,ExecutablePath)
 ConvertTo-Json -InputObject $items -Compress
 exit
}
if ($Action -eq 'stop') {
 $p=Own-Process
 $null=$p.CloseMainWindow()
 if (!$p.WaitForExit(20000)) {
  $p=Own-Process
  Stop-Process -Id $p.Id -ErrorAction Stop
  $p.WaitForExit()
 }
 exit
}
$null=Own-Process
$pmArgs=@('--process_id',"$GameProcessId",'--timed',"$DurationSeconds",'--terminate_after_timed','--session_name',$SessionName,'--output_file',('"'+$CsvPath+'"'))
$timer=[Diagnostics.Stopwatch]::StartNew()
$samples=[Collections.Generic.List[object]]::new()
$collector=$null
try {
 $collector=Start-Process -FilePath $PresentMon -ArgumentList $pmArgs -PassThru -RedirectStandardOutput ($ResultPath+'.stdout.txt') -RedirectStandardError ($ResultPath+'.stderr.txt')
 $threadStart=@{};foreach($thread in (Own-Process).Threads){try{$threadStart[$thread.Id]=$thread.TotalProcessorTime.TotalSeconds}catch{}}
 $threadClockStart=$timer.Elapsed.TotalSeconds
 [DateTime]::UtcNow.ToString('o') | Set-Content -LiteralPath ($ResultPath+'.started') -Encoding utf8
 while ($timer.Elapsed.TotalSeconds -lt $DurationSeconds) {
  $p=Own-Process
  $samples.Add([ordered]@{elapsedSeconds=$timer.Elapsed.TotalSeconds;cpuSeconds=$p.TotalProcessorTime.TotalSeconds;privateBytes=$p.PrivateMemorySize64;workingSetBytes=$p.WorkingSet64;threads=$p.Threads.Count})
  Start-Sleep -Milliseconds 1000
 }
 if (!$collector.WaitForExit(15000)) { throw 'PresentMon failed to finish its timed capture' }
 if ($collector.ExitCode -ne 0) { throw "PresentMon exit code $($collector.ExitCode)" }
 if (!(Test-Path -LiteralPath $CsvPath)) { throw 'PresentMon did not create CSV' }
 $threadElapsed=$timer.Elapsed.TotalSeconds-$threadClockStart
 $threadDeltas=@(foreach($thread in (Own-Process).Threads){try{if($threadStart.ContainsKey($thread.Id)){[ordered]@{id=$thread.Id;coreEquivalent=($thread.TotalProcessorTime.TotalSeconds-$threadStart[$thread.Id])/$threadElapsed}}}catch{}})
 $topThreads=@($threadDeltas | Sort-Object coreEquivalent -Descending | Select-Object -First 10)
 [ordered]@{threadSamplingSeconds=$threadElapsed;topThreads=$topThreads;pid=$GameProcessId;exe=$ExpectedExe;elapsedSeconds=$timer.Elapsed.TotalSeconds;samples=$samples;presentMonExit=$collector.ExitCode} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $ResultPath -Encoding utf8
} finally {
 if ($collector -and !$collector.HasExited) {
  $ownCollector=Get-Process -Id $collector.Id -ErrorAction SilentlyContinue
  if ($ownCollector -and [string]::Equals($ownCollector.Path,$PresentMon,[StringComparison]::OrdinalIgnoreCase)) { Stop-Process -Id $ownCollector.Id }
 }
}

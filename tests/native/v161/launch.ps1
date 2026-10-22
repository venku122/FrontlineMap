param([Parameter(Mandatory=$true)][string]$GameRoot,[string]$Mission='Escalation',[string]$ConnectAddress,[switch]$MenuOnly,[int]$GamePort=46901,[int]$Width=1280,[int]$Height=800,[string]$LogFile)
$ErrorActionPreference='Stop'
$GameRoot=(Resolve-Path -LiteralPath $GameRoot).Path
if (!(Test-Path -LiteralPath (Join-Path $GameRoot '.frontlinemap-disposable') -PathType Leaf)) { throw 'Explicit disposable-copy marker required before launch; this launcher never creates it.' }
if (!(Test-Path -LiteralPath (Join-Path $GameRoot 'NuclearOption.exe') -PathType Leaf)) { throw 'Game executable missing.' }
if ($ConnectAddress -and $ConnectAddress -notmatch '^(localhost|127\.0\.0\.1)(:\d+)?$') { throw 'Only literal loopback connections are permitted.' }

$argsList=@('-socket','UDP','-port',"$GamePort",'-screen-fullscreen','0','-screen-width',"$Width",'-screen-height',"$Height")
if($MenuOnly){}elseif($ConnectAddress){$argsList+=@('-autoConnect',$ConnectAddress)}else{$argsList+=@('-autoHost','-mission',$Mission)}
if($LogFile){$argsList+=@('-logFile',$LogFile)}
$p=Start-Process -FilePath (Join-Path $GameRoot 'NuclearOption.exe') -WorkingDirectory $GameRoot -ArgumentList $argsList -PassThru
$p | Select-Object Id,Path,StartTime | ConvertTo-Json -Compress

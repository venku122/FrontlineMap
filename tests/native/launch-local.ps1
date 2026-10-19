param([Parameter(Mandatory=$true)][string]$GameRoot,[string]$Mission='Escalation',[string]$ConnectAddress,[int]$GamePort=46901,[int]$Width=1280,[int]$Height=800,[string]$LogFile)
$ErrorActionPreference='Stop'
$argsList=@('-socket','UDP','-port',"$GamePort",'-screen-fullscreen','0','-screen-width',"$Width",'-screen-height',"$Height")
if($ConnectAddress){$argsList+=@('-autoConnect',$ConnectAddress)}else{$argsList+=@('-autoHost','-mission',$Mission)}
if($LogFile){$argsList+=@('-logFile',$LogFile)}
$p=Start-Process -FilePath (Join-Path $GameRoot 'NuclearOption.exe') -WorkingDirectory $GameRoot -ArgumentList $argsList -PassThru
$p | Select-Object Id,Path,StartTime | ConvertTo-Json -Compress

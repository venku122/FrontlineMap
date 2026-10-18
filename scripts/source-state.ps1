function Get-SourceFingerprint([string]$Root) {
 $names=@('FrontlineMap.csproj','Plugin.cs')
 $names+=@(Get-ChildItem (Join-Path $Root 'src') -Recurse -File -Filter *.cs | ForEach-Object {$_.FullName.Substring($Root.Length+1).Replace('\','/')})
 $names+=@(Get-ChildItem $Root -File | Where-Object {$_.Name -like '*.props' -or $_.Name -like '*.targets'} | ForEach-Object {$_.Name})
 [Array]::Sort($names,[StringComparer]::Ordinal)
 $lines=@();foreach($name in $names){$lines+=((Get-FileHash -LiteralPath (Join-Path $Root $name) -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$name)}
 $algorithm=[Security.Cryptography.SHA256]::Create()
 try{$hash=[BitConverter]::ToString($algorithm.ComputeHash([Text.Encoding]::UTF8.GetBytes(($lines -join "`n")))).Replace('-','').ToLowerInvariant()}finally{$algorithm.Dispose()}
 return @{tree_sha256=$hash;files=$lines}
}

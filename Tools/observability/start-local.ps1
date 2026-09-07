param([Parameter(Mandatory=$true)][string]$Runtime)
$ErrorActionPreference='Stop'
$runtimePath=(Resolve-Path -LiteralPath $Runtime).Path
$sourcePath=$PSScriptRoot
foreach($port in @(9464,9090,3300)) {
 if(Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue){throw "Port $port is already in use. Stop the existing instance first."}
}
$credentialsPath=Join-Path $runtimePath 'credentials.json'
if(!(Test-Path -LiteralPath $credentialsPath)) {
 $secrets=@{token=[Guid]::NewGuid().ToString('N')+[Guid]::NewGuid().ToString('N');adminPassword=[Guid]::NewGuid().ToString('N')}
 $secrets | ConvertTo-Json | Set-Content -LiteralPath $credentialsPath
}
$credentials=Get-Content -LiteralPath $credentialsPath -Raw | ConvertFrom-Json
$env:GAME_METRICS_TOKEN=$credentials.token
$env:GF_SECURITY_ADMIN_PASSWORD=$credentials.adminPassword
$env:GF_SECURITY_ADMIN_USER='admin'
$env:GF_PLUGINS_PREINSTALL_DISABLED='true'
$pythonPath=Join-Path $runtimePath 'venv/Scripts/python.exe'
& $pythonPath (Join-Path $sourcePath 'configure.py') $runtimePath
if($LASTEXITCODE -ne 0){throw 'Configuration failed'}
$processes=@()
try {
 $processes+=Start-Process -FilePath $pythonPath -ArgumentList @(('"'+$sourcePath+'/collector.py"')) -WindowStyle Hidden -PassThru -RedirectStandardError ($runtimePath+'/logs/collector-error.log')
 $processes+=Start-Process -FilePath ($runtimePath+'/prometheus-3.13.2.windows-amd64/prometheus.exe') -ArgumentList @('--config.file',('"'+$runtimePath+'/prometheus.yml"'),'--storage.tsdb.path',('"'+$runtimePath+'/data/prometheus"'),'--storage.tsdb.retention.time=7d','--storage.tsdb.retention.size=1GB','--web.listen-address=127.0.0.1:9090') -WindowStyle Hidden -PassThru -RedirectStandardError ($runtimePath+'/logs/prometheus.log')
 $processes+=Start-Process -FilePath ($runtimePath+'/grafana-13.2.1/bin/grafana.exe') -ArgumentList @('server','--homepath',('"'+$runtimePath+'/grafana-13.2.1"'),'--config',('"'+$runtimePath+'/grafana.ini"')) -WindowStyle Hidden -PassThru -RedirectStandardOutput ($runtimePath+'/logs/grafana.log') -RedirectStandardError ($runtimePath+'/logs/grafana-error.log')
 $processes | ForEach-Object { @{id=$_.Id;path=$_.Path;started=$_.StartTime.ToUniversalTime().ToString('o')} } | ConvertTo-Json | Set-Content ($runtimePath+'/processes.json')
 Write-Output 'Grafana: http://127.0.0.1:3300/d/game-performance'
 Write-Output "Admin credentials: $credentialsPath (do not commit)"
} catch {
 foreach($p in $processes){if(!$p.HasExited){Stop-Process -Id $p.Id}}
 throw
} finally {
 Remove-Item Env:GAME_METRICS_TOKEN,Env:GF_SECURITY_ADMIN_PASSWORD -ErrorAction SilentlyContinue
}

param([Parameter(Mandatory=$true)][string]$Runtime)
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Force $Runtime | Out-Null
$runtimePath=(Resolve-Path -LiteralPath $Runtime).Path
$packages=@(
 @{name='grafana.tar.gz';url='https://dl.grafana.com/grafana/release/13.2.1/grafana_13.2.1_33191028959_windows_amd64.tar.gz';sha='b6d7060b8d133930742a9326a6065f90e629b173188e041696856b337754a02b'},
 @{name='prometheus.zip';url='https://github.com/prometheus/prometheus/releases/download/v3.13.2/prometheus-3.13.2.windows-amd64.zip';sha='ebd97831d80be097eed54af257816ca7fa93cc67682bdcf94d44057b17cde6ad'}
)
foreach($package in $packages){
 $file=Join-Path $runtimePath $package.name
 if(!(Test-Path -LiteralPath $file)){Invoke-WebRequest $package.url -OutFile $file}
 if((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $package.sha){throw "Checksum mismatch: $file"}
}
tar -xzf ($runtimePath+'/grafana.tar.gz') -C $runtimePath
if($LASTEXITCODE -ne 0){throw 'Grafana extraction failed'}
Expand-Archive -LiteralPath ($runtimePath+'/prometheus.zip') -DestinationPath $runtimePath -Force
python -m venv ($runtimePath+'/venv')
if($LASTEXITCODE -ne 0){throw 'Python venv failed'}
& ($runtimePath+'/venv/Scripts/python.exe') -m pip install -r (Join-Path $PSScriptRoot 'requirements.txt')
if($LASTEXITCODE -ne 0){throw 'Python dependency installation failed'}

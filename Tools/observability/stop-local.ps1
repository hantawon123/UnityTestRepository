param([Parameter(Mandatory=$true)][string]$Runtime)
$runtimePath=(Resolve-Path -LiteralPath $Runtime).Path
foreach($entry in (Get-Content -LiteralPath ($runtimePath+'/processes.json') -Raw | ConvertFrom-Json)) {
 $p=Get-Process -Id $entry.id -ErrorAction SilentlyContinue
 if($p -and $p.Path -eq $entry.path -and $p.StartTime.ToUniversalTime().Ticks -eq ([DateTimeOffset]$entry.started).UtcTicks){
  # Windows venv python.exe starts a child interpreter. Only stop children of our verified process.
  $children=Get-CimInstance Win32_Process -Filter "ParentProcessId=$($p.Id)"
  foreach($child in $children){if($child.CreationDate -ge $p.StartTime){Stop-Process -Id $child.ProcessId -ErrorAction SilentlyContinue}}
  Stop-Process -Id $p.Id -ErrorAction SilentlyContinue
 }
}

$ErrorActionPreference = 'SilentlyContinue'

# Compute Start Menu folder (per-machine)
$menu = Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs\Gaseous Server'
$link = Join-Path $menu 'Gaseous Server Web.url'

# Resolve config path
$cfgPath = $null
$envMachine = [Environment]::GetEnvironmentVariable('GASEOUS_CONFIG_PATH','Machine')
if (-not [string]::IsNullOrWhiteSpace($envMachine)) {
  $p = Join-Path $envMachine 'config.json'
  if (Test-Path $p) { $cfgPath = $p }
}
if (-not $cfgPath) {
  $p = Join-Path $HOME '.gaseous-server\config.json'
  if (Test-Path $p) { $cfgPath = $p }
}

# Determine port
$port = 5198
if ($cfgPath) {
  try {
    $json = Get-Content -Raw -Encoding UTF8 $cfgPath | ConvertFrom-Json
    if ($json.ServerPort -gt 0) { $port = [int]$json.ServerPort }
  } catch {}
}

# Build URL and write .url file
$url = 'http://localhost:' + $port + '/'
$content = "[InternetShortcut]`r`nURL=$url`r`nIconIndex=0`r`n"
New-Item -ItemType Directory -Force -Path $menu | Out-Null
Set-Content -Path $link -Value $content -Encoding ASCII

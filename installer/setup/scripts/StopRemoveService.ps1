param(
  [string]$InstallFolder,
  [string]$ServiceName
)

$ErrorActionPreference = 'SilentlyContinue'

# Lightweight log file in %ProgramData% to confirm execution during MSI uninstall
try {
  $logDir = Join-Path $env:ProgramData 'GaseousServer'
  if (-not (Test-Path $logDir)) { New-Item -Path $logDir -ItemType Directory -Force | Out-Null }
  $logFile = Join-Path $logDir 'Uninstall.log'
  $ts = (Get-Date).ToString('s')
  "[$ts] Starting StopRemoveService with InstallFolder='$InstallFolder' ServiceName='$ServiceName'" | Out-File -FilePath $logFile -Encoding utf8 -Append -ErrorAction SilentlyContinue
} catch {}

function Test-ServiceExists([string]$name) {
  if (-not $name) { return $false }
  try {
    $null = Get-Service -Name $name -ErrorAction Stop
    return $true
  } catch {
    # Fallback to sc query to catch services not visible via Get-Service yet
    $p = Start-Process sc.exe -ArgumentList @('query', $name) -NoNewWindow -PassThru -Wait -RedirectStandardOutput temp:
    return ($p.ExitCode -eq 0)
  }
}

  function Resolve-ServiceName([string]$hint) {
    if ([string]::IsNullOrWhiteSpace($hint)) { return $null }
    # Exact name hit
    try { $svc = Get-Service -Name $hint -ErrorAction Stop; return $svc.Name } catch {}
    # Try by display name
    try {
      $escaped = $hint.Replace('"','\"')
      $q = "DisplayName = '$escaped'"
      $svcObj = Get-CimInstance -ClassName Win32_Service -Filter $q -ErrorAction SilentlyContinue
      if ($svcObj) { return [string]$svcObj.Name }
    } catch {}
    return $null
  }

function Stop-And-Delete([string]$name) {
  if (-not $name) { return }
  if (-not (Test-ServiceExists $name)) { return }
  try {
  try { "$(Get-Date -Format s) Attempting stop/delete of service '$name'" | Out-File -FilePath $logFile -Encoding utf8 -Append -ErrorAction SilentlyContinue } catch {}
    # Stop and remove any dependent services first
    try {
      $esc = $name.Replace("'","''")
      $deps = Get-CimInstance -Query "ASSOCIATORS OF {Win32_Service.Name='$esc'} WHERE AssocClass=Win32_DependentService Role=Antecedent" -ErrorAction SilentlyContinue
      if ($deps) {
        foreach ($d in $deps) {
          try {
            $dn = [string]$d.Name
            if ($dn) {
              $dsvc = Get-Service -Name $dn -ErrorAction SilentlyContinue
              if ($dsvc -and $dsvc.Status -ne 'Stopped') {
                try { Stop-Service -Name $dn -Force -ErrorAction SilentlyContinue } catch {}
                for ($i=0; $i -lt 40; $i++) {
                  try { $dsvc = Get-Service -Name $dn -ErrorAction SilentlyContinue; if (-not $dsvc -or $dsvc.Status -eq 'Stopped') { break } } catch {}
                  Start-Sleep -Milliseconds 500
                }
              }
              # Try to delete dependent service
              try {
                $dobj = Get-CimInstance -ClassName Win32_Service -Filter ("Name = '" + $dn.Replace("'","''") + "'") -ErrorAction SilentlyContinue
                if ($dobj) { $null = Invoke-CimMethod -InputObject $dobj -MethodName Delete -ErrorAction SilentlyContinue }
              } catch {}
            }
          } catch {}
        }
      }
    } catch {}

    $svc = Get-Service -Name $name -ErrorAction SilentlyContinue
    if ($svc -and $svc.Status -ne 'Stopped') {
      try { Stop-Service -Name $name -Force -ErrorAction SilentlyContinue } catch {}
      # Wait up to ~30s
      for ($i=0; $i -lt 60; $i++) {
        try {
          $svc = Get-Service -Name $name -ErrorAction SilentlyContinue
          if (-not $svc -or $svc.Status -eq 'Stopped') { break }
        } catch {}
        Start-Sleep -Milliseconds 500
      }
    }
      # Prefer WMI delete
      try {
        $svcObj = Get-CimInstance -ClassName Win32_Service -Filter ("Name = '" + $name.Replace("'","''") + "'") -ErrorAction SilentlyContinue
        if ($svcObj) {
  $null = Invoke-CimMethod -InputObject $svcObj -MethodName Delete -ErrorAction SilentlyContinue
  # ReturnValue 0 = success, 10 = marked for deletion
          # Wait briefly for SCM to flush
        for ($i=0; $i -lt 40; $i++) {
            if (-not (Test-ServiceExists $name)) { break }
            Start-Sleep -Milliseconds 300
          }
        }
      } catch {}
      # Fallback to sc.exe delete with retries
      if (Test-ServiceExists $name) {
      for ($d=0; $d -lt 5; $d++) {
          & sc.exe delete "$name" | Out-Null
          Start-Sleep -Milliseconds 500
          if (-not (Test-ServiceExists $name)) { break }
          Start-Sleep -Seconds 2
        }
      }
  } catch {}
}

function Get-CandidateNames([string]$inst) {
  $c = New-Object System.Collections.Generic.List[string]
  # Common expected names
  @('GaseousServer','Gaseous Server','gaseous-server','gaseousserver') | ForEach-Object { $c.Add($_) }
  if ($ServiceName) { $c.Add($ServiceName) }

  # Registry by ImagePath
  try {
    $keyPath = 'HKLM:\SYSTEM\CurrentControlSet\Services'
    Get-ChildItem $keyPath | ForEach-Object {
      try {
        $p = Get-ItemProperty -Path $_.PsPath -Name ImagePath -ErrorAction SilentlyContinue
        if ($p -and $p.ImagePath) {
          $img = $p.ImagePath.ToString().Trim('"')
          $img = [Environment]::ExpandEnvironmentVariables($img)
          $pattern = 'gaseous[\s\._-]*server(\.exe)?'
          if (($inst -and $img -like ($inst + '*')) -or ($img -match $pattern)) {
            $c.Add($_.PSChildName)
          }
        }
      } catch {}
    }
  } catch {}

  # WMI/CIM by PathName
  try {
    $svcs = Get-CimInstance -ClassName Win32_Service -ErrorAction SilentlyContinue
    foreach ($s in $svcs) {
      $pn = [string]$s.PathName
      if (-not [string]::IsNullOrWhiteSpace($pn)) {
        $pn2 = $pn.Trim('"')
        $pattern = 'gaseous[\s\._-]*server(\.exe)?'
        if (($inst -and $pn2.StartsWith($inst, [System.StringComparison]::OrdinalIgnoreCase)) -or ($pn2 -match $pattern)) {
          $c.Add([string]$s.Name)
        }
      }
    }
  } catch {}

  return ($c | Sort-Object -Unique)
}

# Normalize install folder
$inst = $InstallFolder
if (-not [string]::IsNullOrWhiteSpace($inst)) {
  $inst = [IO.Path]::GetFullPath($inst)
  if (-not $inst.EndsWith('\')) { $inst += '\' }
}

# First resolve provided hint to canonical service name
$resolved = Resolve-ServiceName -hint $ServiceName
$candidates = @()
if ($resolved) { $candidates += $resolved }
$candidates += Get-CandidateNames -inst $inst
foreach ($n in $candidates) { Stop-And-Delete -name $n }

# Cleanup lingering registry keys for deleted services
try {
  $keyPath = 'HKLM:\SYSTEM\CurrentControlSet\Services'
  foreach ($n in $candidates) {
    if (-not (Test-ServiceExists $n)) {
      $svcKey = Join-Path $keyPath $n
      if (Test-Path $svcKey) {
        try { Remove-Item -Path $svcKey -Recurse -Force -ErrorAction SilentlyContinue } catch {}
      }
    }
  }
} catch {}

try { "$(Get-Date -Format s) Completed StopRemoveService" | Out-File -FilePath $logFile -Encoding utf8 -Append -ErrorAction SilentlyContinue } catch {}

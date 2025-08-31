param(
  [Parameter(Mandatory=$true)][string]$PublishDir,
  [Parameter(Mandatory=$true)][string]$OutFile
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path $PublishDir).Path
Write-Host "Harvesting $root -> $OutFile"

function SanitizeId([string]$s, [int]$maxLen = 60) {
  if ([string]::IsNullOrWhiteSpace($s)) { return 'root' }
  $t = $s -replace "[^A-Za-z0-9_]", "_"
  if ($t.Length -gt $maxLen) { $t = $t.Substring(0,$maxLen) }
  if ($t -match '^[0-9]') { $t = 'x' + $t }
  return $t
}

function HashSuffix([string]$s) {
  $bytes = [System.Text.Encoding]::UTF8.GetBytes($s)
  $md5 = [System.Security.Cryptography.MD5]::Create()
  try {
    $hash = $md5.ComputeHash($bytes)
  } finally {
    $md5.Dispose()
  }
  $hex = [System.BitConverter]::ToString($hash).Replace('-', '')
  return $hex.Substring(0, 8)
}

$lines = New-Object System.Collections.Generic.List[string]
$componentIds = New-Object System.Collections.Generic.List[string]

$null = $lines.Add('<?xml version="1.0" encoding="UTF-8"?>')
$null = $lines.Add('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
$null = $lines.Add('  <Fragment>')
$null = $lines.Add('    <DirectoryRef Id="INSTALLFOLDER">')

function EmitDir([string]$dirPath, [string]$relPath, [string]$indent) {
  $isRoot = [string]::IsNullOrWhiteSpace($relPath)
  $indent2 = $indent
  if (-not $isRoot) {
    $name = Split-Path -Leaf $dirPath
    $dirId = 'dir_' + (SanitizeId $relPath)
  $null = $lines.Add($indent + '<Directory Id="' + $dirId + '" Name="' + $name + '">')
    $indent2 = $indent + '  '
  }

  Get-ChildItem -LiteralPath $dirPath -File | ForEach-Object {
    $fileRel = if ([string]::IsNullOrWhiteSpace($relPath)) { $_.Name } else { Join-Path $relPath $_.Name }
  $hash8 = HashSuffix $fileRel
  # Ensure total id length <= 72: 'cmp_'(4) + base + '_'(1) + hash(8) => base max 59
  $baseId = SanitizeId $fileRel 59
  $compId = 'cmp_' + $baseId + '_' + $hash8
  $fileId = 'fil_' + $baseId + '_' + $hash8
  $src = $_.FullName.Replace('&','&amp;').Replace('<','&lt;').Replace('>','&gt;').Replace('"','&quot;')
  $null = $lines.Add($indent2 + '<Component Id="' + $compId + '" Guid="*">')
  $null = $lines.Add($indent2 + '  <File Id="' + $fileId + '" Source="' + $src + '" />')
  $null = $lines.Add($indent2 + '</Component>')
    $componentIds.Add($compId) | Out-Null
  }

  Get-ChildItem -LiteralPath $dirPath -Directory | ForEach-Object {
    $childRel = if ([string]::IsNullOrWhiteSpace($relPath)) { $_.Name } else { Join-Path $relPath $_.Name }
    EmitDir $_.FullName $childRel $indent2
  }

  if (-not $isRoot) {
  $null = $lines.Add($indent + '</Directory>')
  }
}

EmitDir $root '' '      '

$null = $lines.Add('    </DirectoryRef>')
$null = $lines.Add('    <ComponentGroup Id="AppFiles">')
foreach ($cid in $componentIds) {
  $null = $lines.Add('      <ComponentRef Id="' + $cid + '" />')
}
$null = $lines.Add('    </ComponentGroup>')
$null = $lines.Add('  </Fragment>')
$null = $lines.Add('</Wix>')

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutFile) | Out-Null
$lines | Set-Content -Path $OutFile -Encoding UTF8
Write-Host "Harvest generated: $OutFile"

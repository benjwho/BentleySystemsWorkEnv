#Requires -Version 5.1
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
  [string]$TechRoot = "C:\Users\zohar\Documents\INWC_RH\tech",
  [string[]]$ProductConfigRoots = @(
    "C:\ProgramData\Bentley\MicroStation 2025\Configuration",
    "C:\ProgramData\Bentley\OpenRoads Designer 2025.00\Configuration",
    "C:\ProgramData\Bentley\OpenSite Designer 2025.00\Configuration",
    "C:\ProgramData\Bentley\OpenCities Map Ultimate 2025\Configuration",
    "C:\ProgramData\Bentley\Bentley Descartes 2025\Configuration",
    "C:\ProgramData\Bentley\OpenPlant 2024\Configuration"
  )
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Normalize-CfgRoot {
  param([string]$Value)
  if ([string]::IsNullOrWhiteSpace($Value)) { return $null }

  $normalized = $Value.Trim().Trim('"', "'") -replace "\\", "/"
  $normalized = $normalized.TrimEnd("/")
  if ([string]::IsNullOrWhiteSpace($normalized)) { return $null }
  return ($normalized + "/")
}

function Get-CfgVarValue {
  param(
    [Parameter(Mandatory)] [string]$Path,
    [Parameter(Mandatory)] [string]$Var
  )

  if (!(Test-Path -LiteralPath $Path)) { return $null }

  $rx = "^\s*{0}\s*=\s*(.*?)\s*$" -f [regex]::Escape($Var)
  $last = $null
  foreach ($line in (Get-Content -LiteralPath $Path -ErrorAction Stop)) {
    if ($line -match '^\s*[#;]') { continue }
    if ($line -match $rx) { $last = $Matches[1].Trim() }
  }
  return $last
}

function Set-CfgVarLine {
  param(
    [Parameter(Mandatory)] [string]$Text,
    [Parameter(Mandatory)] [string]$Var,
    [Parameter(Mandatory)] [string]$Value
  )

  $assignment = "{0}={1}" -f $Var, $Value
  $linePattern = "(?m)^\s*{0}\s*=.*$" -f [regex]::Escape($Var)

  if ($Text -match $linePattern) {
    return [regex]::Replace($Text, $linePattern, $assignment, 1)
  }

  $generalPattern = "(?ms)^\s*\[General\]\s*\r?\n"
  if ($Text -match $generalPattern) {
    return [regex]::Replace($Text, $generalPattern, "[General]`r`n$assignment`r`n", 1)
  }

  $trimmed = $Text.TrimEnd("`r", "`n")
  if ([string]::IsNullOrEmpty($trimmed)) {
    return "[General]`r`n$assignment`r`n"
  }

  return "[General]`r`n$assignment`r`n`r`n$trimmed`r`n"
}

$cfgRootWin = Join-Path $TechRoot "Configuration"
$expectedCustom = Normalize-CfgRoot $cfgRootWin

if (-not $expectedCustom) {
  throw "Unable to build expected _USTN_CUSTOM_CONFIGURATION from TechRoot: $TechRoot"
}

$changes = 0

foreach ($productRoot in $ProductConfigRoots) {
  $setupCfg = Join-Path $productRoot "ConfigurationSetup.cfg"

  if (!(Test-Path -LiteralPath $setupCfg)) {
    Write-Warning "Missing ProgramData config (skipped): $setupCfg"
    continue
  }

  $currentRaw = Get-CfgVarValue -Path $setupCfg -Var "_USTN_CUSTOM_CONFIGURATION"
  $currentNorm = Normalize-CfgRoot $currentRaw

  if ($currentNorm -eq $expectedCustom) {
    Write-Host "OK (no change): $setupCfg"
    continue
  }

  $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
  $backupPath = "$setupCfg.bak.$timestamp"

  if ($PSCmdlet.ShouldProcess($setupCfg, "Backup and set _USTN_CUSTOM_CONFIGURATION=$expectedCustom")) {
    Copy-Item -LiteralPath $setupCfg -Destination $backupPath -Force

    $original = Get-Content -LiteralPath $setupCfg -Raw -ErrorAction Stop
    $updated = Set-CfgVarLine -Text $original -Var "_USTN_CUSTOM_CONFIGURATION" -Value $expectedCustom

    if ($updated -ne $original) {
      Set-Content -LiteralPath $setupCfg -Value $updated -Encoding ASCII
      Write-Host "FIXED: $setupCfg"
      Write-Host "Backup: $backupPath"
      $changes++
    } else {
      Write-Host "OK (no change after normalization): $setupCfg"
    }
  }
}

if ($changes -eq 0) {
  Write-Host "No change: all available ProgramData pointers are already correct." -ForegroundColor Green
} else {
  Write-Host ("Applied changes: {0}" -f $changes) -ForegroundColor Green
}

exit 0

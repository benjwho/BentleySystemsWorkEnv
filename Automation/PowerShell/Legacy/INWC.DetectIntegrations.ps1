#Requires -Version 5.1
[CmdletBinding()]
param(
  [string]$TechRoot = "C:\Users\zohar\Documents\INWC_RH\tech",
  [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-CfgVarValue {
  param(
    [Parameter(Mandatory)] [string]$Path,
    [Parameter(Mandatory)] [string]$Var
  )
  if (!(Test-Path $Path)) { return $null }

  $rx = "^\s*{0}\s*=\s*(.*)\s*$" -f [regex]::Escape($Var)
  foreach ($line in (Get-Content -Path $Path)) {
    if ($line -match $rx) { return $Matches[1].Trim() }
  }
  return $null
}

function Set-CfgVarValueText {
  param(
    [Parameter(Mandatory)] [string]$Text,
    [Parameter(Mandatory)] [string]$Var,
    [Parameter(Mandatory)] [string]$Value
  )

  $replacement = "{0} = {1}" -f $Var.PadRight(30), $Value
  $pattern = "(?m)^\s*{0}\s*=.*$" -f [regex]::Escape($Var)

  if ($Text -match $pattern) {
    return [regex]::Replace($Text, $pattern, $replacement, 1)
  }

  $trimmed = $Text.TrimEnd("`r", "`n")
  if ([string]::IsNullOrEmpty($trimmed)) {
    return ($replacement + "`r`n")
  }
  return ($trimmed + "`r`n" + $replacement + "`r`n")
}

$interopCfg = Join-Path $TechRoot "Configuration\Organization\Interoperability\Interoperability.cfg"
$logRoot = Join-Path $TechRoot "Logs\NWC_Rehab"

if (!(Test-Path $interopCfg)) {
  throw "Interoperability config not found: $interopCfg"
}

if (!(Test-Path $logRoot)) {
  New-Item -Path $logRoot -ItemType Directory -Force | Out-Null
}

$detections = @(
  [pscustomobject]@{
    Var = "INWC_INTEROP_OPENFLOWS"
    Paths = @(
      "C:\Program Files\Bentley\FlowMaster\FlowMaster.exe",
      "C:\Program Files\Bentley\OpenFlows Water\WaterCAD.exe",
      "C:\Program Files\Bentley\OpenFlows Water\WaterGEMS.exe",
      "C:\Program Files\Bentley\OpenFlows Water\Hamm.exe"
    )
    ForceValue = $null
  },
  [pscustomobject]@{
    Var = "INWC_INTEROP_SYNCHRO"
    Paths = @("C:\Program Files\Bentley\SYNCHRO\4D Pro\Synchro4DPro.exe")
    ForceValue = $null
  },
  [pscustomobject]@{
    Var = "INWC_INTEROP_STRUCTURAL"
    Paths = @(
      "C:\Program Files\Bentley\Engineering\STAAD.Pro 2025\STAAD\Bentley.Staad.exe",
      "C:\Program Files\Bentley\AutoPIPE 2025\autopipe.exe",
      "C:\Program Files\Bentley\Engineering\RCDC 2023\RCDC.exe",
      "C:\Program Files\Bentley\Adina\25.00\bin\aui.exe"
    )
    ForceValue = $null
  },
  [pscustomobject]@{
    Var = "INWC_INTEROP_GEOTECH"
    Paths = @("C:\Program Files\Bentley\Geotechnical\PLAXIS LE CONNECT Edition V21\PLAXISLE.exe")
    ForceValue = $null
  },
  [pscustomobject]@{
    Var = "INWC_INTEROP_PYTHON_AUTOMATION"
    Paths = @("C:\ProgramData\Bentley\PowerPlatformPython\python\python.exe")
    ForceValue = $null
  },
  [pscustomobject]@{
    Var = "INWC_INTEROP_OPENPLANT"
    Paths = @(
      "C:\Program Files\Bentley\OpenPlant 2024\OpenPlantModeler\OpenPlantModeler.exe",
      "C:\Program Files\Bentley\OpenPlant 2024\IsometricsManager\OpenPlantIsoExtractor.exe"
    )
    ForceValue = $null
  },
  [pscustomobject]@{
    Var = "INWC_INTEROP_OPENCITIES"
    Paths = @("C:\Program Files\Bentley\OpenCities Map Ultimate 2025\MapUltimate\MapUltimate.exe")
    ForceValue = $null
  },
  [pscustomobject]@{
    Var = "INWC_INTEROP_DESCARTES"
    Paths = @("C:\Program Files\Bentley\Bentley Descartes 2025\DescartesStandAlone\DescartesStandAlone.exe")
    ForceValue = $null
  },
  [pscustomobject]@{
    Var = "INWC_INTEROP_ITWIN_CAPTURE"
    Paths = @("C:\Program Files\Bentley\iTwin Capture Manage And Extract 25.00.04.01\program\bin\Orbit.exe")
    ForceValue = $null
  },
  [pscustomobject]@{
    Var = "INWC_INTEROP_PROJECTWISE_DRIVE"
    Paths = @("C:\Program Files\Bentley\ProjectWise Drive\ProjectWise Drive.exe")
    ForceValue = $null
  }
)

$cfgText = Get-Content -Path $interopCfg -Raw

$results = New-Object System.Collections.Generic.List[object]
$changeCount = 0

foreach ($d in $detections) {
  $existing = Get-CfgVarValue -Path $interopCfg -Var $d.Var
  $foundCount = @($d.Paths | Where-Object { Test-Path $_ }).Count
  $requiredCount = @($d.Paths).Count
  $allFound = ($foundCount -eq $requiredCount)

  $target = if ($null -ne $d.ForceValue) { $d.ForceValue } elseif ($allFound) { "1" } else { "0" }
  $changed = ($existing -ne $target)
  if ($changed) { $changeCount++ }

  if ($Apply -and $changed) {
    $cfgText = Set-CfgVarValueText -Text $cfgText -Var $d.Var -Value $target
  }

  $results.Add([pscustomobject]@{
    Variable = $d.Var
    Current = $existing
    Target = $target
    Changed = $changed
    Found = "$foundCount/$requiredCount"
    Mode = if ($null -ne $d.ForceValue) { "Forced" } else { "Detected" }
  })
}

if ($Apply) {
  Set-Content -Path $interopCfg -Value $cfgText -Encoding ascii
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss-fff"
$logPath = Join-Path $logRoot ("IntegrationDetect_{0}.txt" -f $timestamp)
$runMode = if ($Apply) { "apply" } else { "dry-run" }

$logLines = @()
$logLines += "INWC integration detection"
$logLines += "timestamp: $((Get-Date).ToString('s'))"
$logLines += "mode: $runMode"
$logLines += "tech_root: $TechRoot"
$logLines += "interop_cfg: $interopCfg"
$logLines += ""
$logLines += ($results | Format-Table -AutoSize | Out-String).TrimEnd()
$logLines += ""
$logLines += ("changes_detected: {0}" -f $changeCount)
$logLines += ("changes_applied: {0}" -f $(if ($Apply) { $changeCount } else { 0 }))
$logLines += ""
$logLines += "ProjectWise Drive follows detection and is enabled when path checks pass."
$logLines | Set-Content -Path $logPath -Encoding ascii

$results | Format-Table -AutoSize
Write-Host ""
Write-Host ("Run mode: {0}" -f $runMode)
Write-Host ("Changes detected: {0}" -f $changeCount)
Write-Host ("Log: {0}" -f $logPath)

exit 0

#Requires -Version 5.1
[CmdletBinding()]
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

function Normalize-CfgPath([string]$p) {
  if (-not $p) { return $null }
  $x = $p.Trim() -replace "\\","/"
  # normalize trailing slash for Bentley cfg roots
  if (-not $x.EndsWith("/")) { $x += "/" }
  return $x
}

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

$customCfgWin = Join-Path $TechRoot "Configuration"
$expectedCustomFwd = Normalize-CfgPath (($customCfgWin -replace "\\","/"))

$expectedLocal = @(
  $TechRoot,
  $customCfgWin,
  (Join-Path $customCfgWin "WorkSpaceSetup.cfg"),
  (Join-Path $customCfgWin "WorkSpaces\INWC_RH.cfg"),
  (Join-Path $customCfgWin "WorkSets\NWC_Rehab.cfg")
)

$results = New-Object System.Collections.Generic.List[object]

# A) Local checks
foreach ($p in $expectedLocal) {
  $results.Add([pscustomobject]@{
    Scope  = "Local"
    Check  = "Path exists"
    Target = $p
    OK     = (Test-Path $p)
    Detail = ""
  })
}

# B) ProgramData checks
foreach ($root in $ProductConfigRoots) {
  $cfgSetup = Join-Path $root "ConfigurationSetup.cfg"
  $hasCfg = Test-Path $cfgSetup

  $results.Add([pscustomobject]@{
    Scope  = "ProgramData"
    Check  = "ConfigSetup exists"
    Target = $cfgSetup
    OK     = $hasCfg
    Detail = ""
  })

  $valRaw = if ($hasCfg) { Get-CfgVarValue -Path $cfgSetup -Var "_USTN_CUSTOM_CONFIGURATION" } else { $null }
  $valNorm = Normalize-CfgPath $valRaw

  $results.Add([pscustomobject]@{
    Scope  = "ProgramData"
    Check  = "_USTN_CUSTOM_CONFIGURATION matches (normalized w/ trailing /)"
    Target = $root
    OK     = ($valNorm -eq $expectedCustomFwd)
    Detail = $valRaw
  })

  # If Bentley concatenates ".../Configuration/" + "WorkSpaceSetup.cfg", it should exist.
  $valAsWin = if ($valNorm) { ($valNorm -replace "/","\").Trim() } else { $null }
  if ($valAsWin -and (-not $valAsWin.EndsWith("\"))) { $valAsWin += "\" }
  $computedWsSetup = if ($valAsWin) { $valAsWin + "WorkSpaceSetup.cfg" } else { $null }

  $results.Add([pscustomobject]@{
    Scope  = "ProgramData"
    Check  = "Computed include hits WorkSpaceSetup.cfg"
    Target = $computedWsSetup
    OK     = ($computedWsSetup -and (Test-Path $computedWsSetup))
    Detail = "Bentley includes: `$(_USTN_CONFIGURATION)WorkSpaceSetup.cfg (resolved by concatenation)"
  })
}

$results | Format-Table -AutoSize

if ($results.OK -contains $false) {
  Write-Host "`nFAIL: At least one check failed." -ForegroundColor Red
  exit 1
} else {
  Write-Host "`nPASS: checks consistent." -ForegroundColor Green
  exit 0
}

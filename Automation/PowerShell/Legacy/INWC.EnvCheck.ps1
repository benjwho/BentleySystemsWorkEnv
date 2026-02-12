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

function Add-Result {
  param(
    [System.Collections.Generic.List[object]]$Bucket,
    [string]$Scope,
    [string]$Check,
    [string]$Target,
    [bool]$OK,
    [string]$Detail = ""
  )

  $Bucket.Add([pscustomobject]@{
    Scope  = $Scope
    Check  = $Check
    Target = $Target
    OK     = $OK
    Detail = $Detail
  }) | Out-Null
}

$results = New-Object System.Collections.Generic.List[object]

$cfgRootWin = Join-Path $TechRoot "Configuration"
$workSpaceSetupPath = Join-Path $cfgRootWin "WorkSpaceSetup.cfg"
$workSpacesCfg = Join-Path $cfgRootWin "WorkSpaces\INWC_RH.cfg"
$workSetsCfg = Join-Path $cfgRootWin "WorkSets\NWC_Rehab.cfg"

$expectedCfgRoot = Normalize-CfgRoot $cfgRootWin
$expectedWorkSpacesRoot = Normalize-CfgRoot (Join-Path $cfgRootWin "WorkSpaces")
$expectedWorkSetsRoot = Normalize-CfgRoot (Join-Path $cfgRootWin "WorkSets")

# A) Local required paths
$localRequired = @(
  $TechRoot,
  $cfgRootWin,
  $workSpaceSetupPath,
  $workSpacesCfg,
  $workSetsCfg
)

foreach ($path in $localRequired) {
  Add-Result -Bucket $results -Scope "Local" -Check "Path exists" -Target $path -OK (Test-Path -LiteralPath $path)
}

# B) WorkSpaceSetup picker variables
if (Test-Path -LiteralPath $workSpaceSetupPath) {
  $myWorkSpaces = Get-CfgVarValue -Path $workSpaceSetupPath -Var "MY_WORKSPACES_LOCATION"
  $myWorkSets = Get-CfgVarValue -Path $workSpaceSetupPath -Var "MY_WORKSET_LOCATION"
  $ustnWorkSpaces = Get-CfgVarValue -Path $workSpaceSetupPath -Var "_USTN_WORKSPACESROOT"
  $ustnWorkSets = Get-CfgVarValue -Path $workSpaceSetupPath -Var "_USTN_WORKSETSROOT"

  $myWorkSpacesNorm = Normalize-CfgRoot $myWorkSpaces
  $myWorkSetsNorm = Normalize-CfgRoot $myWorkSets

  Add-Result -Bucket $results -Scope "WorkSpaceSetup" -Check "MY_WORKSPACES_LOCATION normalized" -Target $expectedWorkSpacesRoot -OK ($myWorkSpacesNorm -eq $expectedWorkSpacesRoot) -Detail $myWorkSpaces
  Add-Result -Bucket $results -Scope "WorkSpaceSetup" -Check "MY_WORKSET_LOCATION normalized" -Target $expectedWorkSetsRoot -OK ($myWorkSetsNorm -eq $expectedWorkSetsRoot) -Detail $myWorkSets

  $wsRootOk = ($ustnWorkSpaces -eq '$(MY_WORKSPACES_LOCATION)') -or ((Normalize-CfgRoot $ustnWorkSpaces) -eq $expectedWorkSpacesRoot)
  $wsetRootOk = ($ustnWorkSets -eq '$(MY_WORKSET_LOCATION)') -or ((Normalize-CfgRoot $ustnWorkSets) -eq $expectedWorkSetsRoot)

  Add-Result -Bucket $results -Scope "WorkSpaceSetup" -Check "_USTN_WORKSPACESROOT resolves" -Target $expectedWorkSpacesRoot -OK $wsRootOk -Detail $ustnWorkSpaces
  Add-Result -Bucket $results -Scope "WorkSpaceSetup" -Check "_USTN_WORKSETSROOT resolves" -Target $expectedWorkSetsRoot -OK $wsetRootOk -Detail $ustnWorkSets
}

# C) ProgramData pointers (or user env fallback)
$userEnvCustom = Normalize-CfgRoot ([Environment]::GetEnvironmentVariable("_USTN_CUSTOM_CONFIGURATION", "User"))
$machineEnvCustom = Normalize-CfgRoot ([Environment]::GetEnvironmentVariable("_USTN_CUSTOM_CONFIGURATION", "Machine"))
$envPointerOk = ($userEnvCustom -eq $expectedCfgRoot) -or ($machineEnvCustom -eq $expectedCfgRoot)

Add-Result -Bucket $results -Scope "Environment" -Check "User/Machine _USTN_CUSTOM_CONFIGURATION" -Target $expectedCfgRoot -OK $envPointerOk -Detail ("User={0}; Machine={1}" -f $userEnvCustom, $machineEnvCustom)

foreach ($productRoot in $ProductConfigRoots) {
  $setupCfg = Join-Path $productRoot "ConfigurationSetup.cfg"
  $exists = Test-Path -LiteralPath $setupCfg
  Add-Result -Bucket $results -Scope "ProgramData" -Check "ConfigurationSetup exists" -Target $setupCfg -OK $exists

  if (-not $exists) { continue }

  $rawCustom = Get-CfgVarValue -Path $setupCfg -Var "_USTN_CUSTOM_CONFIGURATION"
  $rawCustomNorm = Normalize-CfgRoot $rawCustom

  $pointerOk = ($rawCustomNorm -eq $expectedCfgRoot) -or ((-not $rawCustomNorm) -and $envPointerOk)
  Add-Result -Bucket $results -Scope "ProgramData" -Check "_USTN_CUSTOM_CONFIGURATION points to INWC cfg" -Target $productRoot -OK $pointerOk -Detail ("Config={0}; EnvFallback={1}" -f $rawCustomNorm, $envPointerOk)

  $effectivePointer = if ($rawCustomNorm) { $rawCustomNorm } elseif ($envPointerOk) { $expectedCfgRoot } else { $null }
  $computedInclude = if ($effectivePointer) { ($effectivePointer -replace "/", "\\") + "WorkSpaceSetup.cfg" } else { $null }

  Add-Result -Bucket $results -Scope "ProgramData" -Check "Computed include resolves WorkSpaceSetup.cfg" -Target $computedInclude -OK ($computedInclude -and (Test-Path -LiteralPath $computedInclude)) -Detail "Bentley include: `$(_USTN_CONFIGURATION)WorkSpaceSetup.cfg"
}

$results | Format-Table -AutoSize

if ($results.OK -contains $false) {
  Write-Host "`nFAIL: environment checks found issues." -ForegroundColor Red
  exit 1
}

Write-Host "`nPASS: environment checks passed." -ForegroundColor Green
exit 0

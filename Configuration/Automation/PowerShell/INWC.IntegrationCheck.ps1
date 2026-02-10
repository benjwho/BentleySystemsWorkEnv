#Requires -Version 5.1
[CmdletBinding()]
param(
  [string]$TechRoot = "C:\Users\zohar\Documents\INWC_RH\tech"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Normalize-Path([string]$p) {
  if (-not $p) { return $null }
  return ($p.Trim() -replace "/", "\")
}

function Get-CfgVarValue {
  param(
    [Parameter(Mandatory)] [string]$Path,
    [Parameter(Mandatory)] [string]$Var
  )
  if (!(Test-Path $Path)) { return $null }

  $rx = "^\s*{0}\s*=\s*(.*)\s*$" -f [regex]::Escape($Var)
  foreach ($line in (Get-Content -Path $Path)) {
    if ($line -match $rx) {
      return $Matches[1].Trim()
    }
  }
  return $null
}

function Resolve-CfgTokens {
  param(
    [Parameter(Mandatory)] [string]$Value,
    [Parameter(Mandatory)] [hashtable]$Tokens
  )
  $resolved = $Value
  foreach ($k in $Tokens.Keys) {
    $resolved = $resolved -replace [regex]::Escape("`$($k)"), $Tokens[$k]
  }
  return (Normalize-Path $resolved)
}

$cfgRoot = Join-Path $TechRoot "Configuration"
$projectRoot = Join-Path $TechRoot "Projects\NWC_Rehab"

$tokens = @{
  "INWC_TECH" = $TechRoot
  "INWC_CFG" = $cfgRoot
  "INWC_PROJECT_ROOT" = $projectRoot
  "INWC_EXPORT" = (Join-Path $projectRoot "Exports")
  "INWC_DATA" = (Join-Path $TechRoot "Data")
}

$orgCfg = Join-Path $cfgRoot "Organization\ByProject\NWC_Rehab\NWC_Rehab_Organization.cfg"
$interopCfg = Join-Path $cfgRoot "Organization\Interoperability\Interoperability.cfg"
$civilCfg = Join-Path $cfgRoot "Organization\ByDiscipline\Civil\Civil_Standards.cfg"
$structCfg = Join-Path $cfgRoot "Organization\ByDiscipline\Structures\Structural_Standards.cfg"
$geotechCfg = Join-Path $cfgRoot "Organization\ByDiscipline\Geotechnical\Geotech_Standards.cfg"
$utilCfg = Join-Path $cfgRoot "Organization\ByDiscipline\Utilities\Utilities_Standards.cfg"
$plantCfg = Join-Path $cfgRoot "Organization\ByDiscipline\Plant\OpenPlant_Standards.cfg"
$synCfg = Join-Path $cfgRoot "Organization\ByFunction\Scheduling\SYNCHRO_Integration.cfg"
$geoCfg = Join-Path $cfgRoot "Organization\ByFunction\Geospatial\OpenCities_Integration.cfg"
$descCfg = Join-Path $cfgRoot "Organization\ByFunction\Reality\Descartes_Integration.cfg"
$itwinCfg = Join-Path $cfgRoot "Organization\ByFunction\Reality\iTwinCapture_Integration.cfg"
$docMgmtCfg = Join-Path $cfgRoot "Organization\ByFunction\DocumentManagement\ProjectWiseDrive_Integration.cfg"
$automationCfg = Join-Path $cfgRoot "Organization\ByFunction\Automation\Python_Integration.cfg"

$results = New-Object System.Collections.Generic.List[object]

function Add-Result {
  param(
    [string]$Scope,
    [string]$Check,
    [string]$Target,
    [bool]$OK,
    [string]$Detail = ""
  )
  $results.Add([pscustomobject]@{
    Scope = $Scope
    Check = $Check
    Target = $Target
    OK = $OK
    Detail = $Detail
  })
}

# A) Integration config files
$cfgFiles = @(
  $orgCfg,
  $interopCfg,
  $civilCfg,
  $structCfg,
  $geotechCfg,
  $utilCfg,
  $plantCfg,
  $synCfg,
  $geoCfg,
  $descCfg,
  $itwinCfg,
  $docMgmtCfg,
  $automationCfg
)
foreach ($f in $cfgFiles) {
  Add-Result -Scope "Config" -Check "File exists" -Target $f -OK (Test-Path $f)
}

# B) Include chain variable resolution in project organization config
$includeVars = @(
  "INTEROPERABILITY_CONFIG",
  "DISCIPLINE_CIVIL_CONFIG",
  "DISCIPLINE_STRUCTURES_CONFIG",
  "DISCIPLINE_GEOTECHNICAL_CONFIG",
  "DISCIPLINE_UTILITIES_CONFIG",
  "DISCIPLINE_PLANT_CONFIG",
  "FUNCTION_SCHEDULING_CONFIG",
  "FUNCTION_GEOSPATIAL_CONFIG",
  "FUNCTION_REALITY_DESCARTES_CONFIG",
  "FUNCTION_REALITY_ITWIN_CAPTURE_CONFIG",
  "FUNCTION_DOCUMENT_MANAGEMENT_CONFIG",
  "FUNCTION_AUTOMATION_CONFIG"
)

foreach ($v in $includeVars) {
  $raw = Get-CfgVarValue -Path $orgCfg -Var $v
  $resolved = if ($raw) { Resolve-CfgTokens -Value $raw -Tokens $tokens } else { $null }
  $ok = ($resolved -and (Test-Path $resolved))
  Add-Result -Scope "IncludeVar" -Check $v -Target $resolved -OK $ok -Detail $raw
}

$orgText = if (Test-Path $orgCfg) { Get-Content -Path $orgCfg -Raw } else { "" }
foreach ($v in $includeVars) {
  $pattern = "(?m)^\s*%include\s+\$\({0}\)\s*$" -f [regex]::Escape($v)
  $hasInclude = ($orgText -match $pattern)
  Add-Result -Scope "IncludeLine" -Check ('%include $(' + $v + ')') -Target $orgCfg -OK $hasInclude
}

# C) Interoperability toggle values
$toggleVars = @(
  "INWC_INTEROP_OPENFLOWS",
  "INWC_INTEROP_SYNCHRO",
  "INWC_INTEROP_STRUCTURAL",
  "INWC_INTEROP_GEOTECH",
  "INWC_INTEROP_OPENPLANT",
  "INWC_INTEROP_OPENCITIES",
  "INWC_INTEROP_DESCARTES",
  "INWC_INTEROP_ITWIN_CAPTURE",
  "INWC_INTEROP_PROJECTWISE_DRIVE",
  "INWC_INTEROP_PYTHON_AUTOMATION",
  "INWC_INTEROP_ISM_SYNC",
  "INWC_INTEROP_WATERCAD_DGN_LINK",
  "INWC_INTEROP_HAMMER_DGN_LINK",
  "INWC_INTEROP_SCHEDULE_LINK"
)

foreach ($v in $toggleVars) {
  $val = Get-CfgVarValue -Path $interopCfg -Var $v
  $ok = ($val -eq "0" -or $val -eq "1")
  Add-Result -Scope "Toggles" -Check $v -Target $interopCfg -OK $ok -Detail $val
}

# D) Required integration folders
$requiredFolders = @(
  (Join-Path $projectRoot "Analysis\Water"),
  (Join-Path $projectRoot "Reports\Hydraulic"),
  (Join-Path $projectRoot "Exports\SYNCHRO"),
  (Join-Path $projectRoot "Analysis\Structural"),
  (Join-Path $projectRoot "Analysis\Geotechnical"),
  (Join-Path $projectRoot "Analysis\PipeStress"),
  (Join-Path $projectRoot "Models\Plant"),
  (Join-Path $projectRoot "Exports\OpenPlant\Isometrics"),
  (Join-Path $projectRoot "Analysis\Geospatial"),
  (Join-Path $projectRoot "Exports\OpenCities"),
  (Join-Path $projectRoot "Analysis\Reality\RasterProcessed"),
  (Join-Path $projectRoot "Exports\iTwinCapture"),
  (Join-Path $projectRoot "DocumentManagement\ProjectWiseDrive"),
  (Join-Path $cfgRoot "Automation\Python"),
  (Join-Path $TechRoot "Data\Geotechnical"),
  (Join-Path $TechRoot "Data\ISM_Models"),
  (Join-Path $TechRoot "Data\Plant\Catalogs"),
  (Join-Path $TechRoot "Data\GIS"),
  (Join-Path $TechRoot "Data\Reality\Raster"),
  (Join-Path $TechRoot "Data\Reality\iTwinCapture"),
  (Join-Path $TechRoot "Data\ProjectWiseDrive\Sync"),
  (Join-Path $TechRoot "Data\ProjectWiseDrive\Cache"),
  (Join-Path $TechRoot "Logs\NWC_Rehab\Python"),
  (Join-Path $cfgRoot "Standards\STAAD\Templates")
)

foreach ($d in $requiredFolders) {
  Add-Result -Scope "Folders" -Check "Path exists" -Target $d -OK (Test-Path $d)
}

# E) Executable paths from integration configs
$exeVars = @(
  @{ Cfg = $utilCfg; Var = "FLOWMASTER_EXE" },
  @{ Cfg = $utilCfg; Var = "WATERCAD_EXE" },
  @{ Cfg = $utilCfg; Var = "WATERGEMS_EXE" },
  @{ Cfg = $utilCfg; Var = "HAMMER_EXE" },
  @{ Cfg = $synCfg; Var = "SYNCHRO_EXE_PATH" },
  @{ Cfg = $structCfg; Var = "STAAD_EXE" },
  @{ Cfg = $structCfg; Var = "AUTOPIPE_EXE" },
  @{ Cfg = $structCfg; Var = "RCDC_EXE" },
  @{ Cfg = $structCfg; Var = "ADINA_UI_EXE" },
  @{ Cfg = $geotechCfg; Var = "PLAXIS_LE_EXE" },
  @{ Cfg = $plantCfg; Var = "OPENPLANT_EXE" },
  @{ Cfg = $plantCfg; Var = "OPENPLANT_ISO_EXTRACTOR_EXE" },
  @{ Cfg = $geoCfg; Var = "OPENCITIES_EXE" },
  @{ Cfg = $descCfg; Var = "DESCARTES_EXE" },
  @{ Cfg = $itwinCfg; Var = "ITWIN_CAPTURE_EXE" },
  @{ Cfg = $docMgmtCfg; Var = "PROJECTWISE_DRIVE_EXE" },
  @{ Cfg = $automationCfg; Var = "PYTHON_EXE" }
)

foreach ($item in $exeVars) {
  $raw = Get-CfgVarValue -Path $item.Cfg -Var $item.Var
  $resolved = if ($raw) { Resolve-CfgTokens -Value $raw -Tokens $tokens } else { $null }
  $ok = ($resolved -and (Test-Path $resolved))
  Add-Result -Scope "Executables" -Check $item.Var -Target $resolved -OK $ok -Detail $raw
}

# F) ProjectWise Drive policy (internal consistency)
$pwToggle = Get-CfgVarValue -Path $interopCfg -Var "INWC_INTEROP_PROJECTWISE_DRIVE"
$pwEnabled = ($pwToggle -eq "1")

$pwFunctionVar = Get-CfgVarValue -Path $orgCfg -Var "FUNCTION_DOCUMENT_MANAGEMENT_CONFIG"
$pwResolvedCfg = if ($pwFunctionVar) { Resolve-CfgTokens -Value $pwFunctionVar -Tokens $tokens } else { $null }
$pwCfgOk = ($pwResolvedCfg -and (Test-Path $pwResolvedCfg))
Add-Result -Scope "Policy" -Check "ProjectWise config variable resolves" -Target $pwResolvedCfg -OK $pwCfgOk -Detail $pwFunctionVar

$hasPwInclude = ($orgText -match '(?m)^\s*%include\s+\$\(FUNCTION_DOCUMENT_MANAGEMENT_CONFIG\)\s*$')
Add-Result -Scope "Policy" -Check "ProjectWise include present" -Target $orgCfg -OK $hasPwInclude

$pwDriveEnabledRaw = Get-CfgVarValue -Path $docMgmtCfg -Var "PROJECTWISE_DRIVE_ENABLED"
$pwDriveEnabledUsesToggle = ($pwDriveEnabledRaw -eq '$(INWC_INTEROP_PROJECTWISE_DRIVE)')
Add-Result -Scope "Policy" -Check "PROJECTWISE_DRIVE_ENABLED uses interoperability toggle" -Target $docMgmtCfg -OK $pwDriveEnabledUsesToggle -Detail $pwDriveEnabledRaw

$pwExeRaw = Get-CfgVarValue -Path $docMgmtCfg -Var "PROJECTWISE_DRIVE_EXE"
$pwExeResolved = if ($pwExeRaw) { Resolve-CfgTokens -Value $pwExeRaw -Tokens $tokens } else { $null }
$pwExeExists = ($pwExeResolved -and (Test-Path $pwExeResolved))
$pwConsistencyOk = if ($pwEnabled) { $pwCfgOk -and $hasPwInclude -and $pwExeExists } else { $pwCfgOk -and $hasPwInclude }
Add-Result -Scope "Policy" -Check "ProjectWise toggle internally consistent" -Target $interopCfg -OK $pwConsistencyOk -Detail ("INWC_INTEROP_PROJECTWISE_DRIVE={0}; EXE_EXISTS={1}" -f $pwToggle, $pwExeExists)

$pwServerMode = Get-CfgVarValue -Path $orgCfg -Var "PROJECTWISE_ENABLED"
$pwServerModeOk = ($null -eq $pwServerMode -or $pwServerMode -ne "1")
Add-Result -Scope "Policy" -Check "ProjectWise server mode remains explicit/manual" -Target $orgCfg -OK $pwServerModeOk -Detail "PROJECTWISE_ENABLED=$pwServerMode"

$results | Format-Table -AutoSize

if ($results.OK -contains $false) {
  Write-Host "`nFAIL: integration checks found issues." -ForegroundColor Red
  exit 1
}

Write-Host "`nPASS: integration checks passed." -ForegroundColor Green
exit 0

#Requires -Version 5.1
[CmdletBinding()]
param(
  [string]$TechRoot = "C:\Users\zohar\Documents\INWC_RH\tech"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Normalize-PathForTest {
  param([string]$Value)
  if ([string]::IsNullOrWhiteSpace($Value)) { return $null }

  $normalized = $Value.Trim().Trim('"', "'") -replace "/", "\\"
  return $normalized
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
    if ($line -match $rx) {
      $last = $Matches[1].Trim()
    }
  }

  return $last
}

function Resolve-CfgTokens {
  param(
    [string]$Value,
    [hashtable]$Tokens
  )

  if ([string]::IsNullOrWhiteSpace($Value)) { return $null }

  $resolved = $Value
  for ($i = 0; $i -lt 8; $i++) {
    $next = [regex]::Replace(
      $resolved,
      '\$\(([A-Za-z0-9_]+)\)',
      {
        param($m)
        $name = $m.Groups[1].Value
        if ($Tokens.ContainsKey($name)) { return $Tokens[$name] }
        return $m.Value
      }
    )

    if ($next -eq $resolved) { break }
    $resolved = $next
  }

  return (Normalize-PathForTest $resolved)
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

$cfgRoot = Join-Path $TechRoot "Configuration"
$projectRoot = Join-Path $TechRoot "Projects\NWC_Rehab"

$tokens = @{
  "INWC_TECH" = $TechRoot
  "INWC_CFG" = $cfgRoot
  "INWC_PROJECT_ROOT" = $projectRoot
  "INWC_EXPORT" = (Join-Path $projectRoot "Exports")
  "INWC_DATA" = (Join-Path $TechRoot "Data")
  "INWC_LOGS" = (Join-Path $TechRoot "Logs")
}

$orgCfg = Join-Path $cfgRoot "Organization\ByProject\NWC_Rehab\NWC_Rehab_Organization.cfg"
$interopCfg = Join-Path $cfgRoot "Organization\Interoperability\Interoperability.cfg"

$results = New-Object System.Collections.Generic.List[object]
$installedTools = New-Object System.Collections.Generic.List[object]

Add-Result -Bucket $results -Scope "Config" -Check "Project organization config exists" -Target $orgCfg -OK (Test-Path -LiteralPath $orgCfg)
Add-Result -Bucket $results -Scope "Config" -Check "Interoperability config exists" -Target $interopCfg -OK (Test-Path -LiteralPath $interopCfg)

if (!(Test-Path -LiteralPath $orgCfg) -or !(Test-Path -LiteralPath $interopCfg)) {
  $results | Format-Table -AutoSize
  Write-Host "`nFAIL: required config files are missing." -ForegroundColor Red
  exit 1
}

$orgText = Get-Content -LiteralPath $orgCfg -Raw -ErrorAction Stop

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

$resolvedIncludePaths = @{}
foreach ($var in $includeVars) {
  $raw = Get-CfgVarValue -Path $orgCfg -Var $var
  $resolved = Resolve-CfgTokens -Value $raw -Tokens $tokens
  $ok = ($resolved -and (Test-Path -LiteralPath $resolved))

  Add-Result -Bucket $results -Scope "IncludeVar" -Check $var -Target $resolved -OK $ok -Detail $raw

  $includePattern = "(?m)^\s*%include\s+\$\({0}\)\s*$" -f [regex]::Escape($var)
  Add-Result -Bucket $results -Scope "IncludeLine" -Check ("%include $('{0}')" -f $var) -Target $orgCfg -OK ($orgText -match $includePattern)

  $resolvedIncludePaths[$var] = $resolved
}

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

$toggleValues = @{}
foreach ($toggle in $toggleVars) {
  $val = Get-CfgVarValue -Path $interopCfg -Var $toggle
  $toggleValues[$toggle] = $val
  Add-Result -Bucket $results -Scope "Toggle" -Check $toggle -Target $interopCfg -OK ($val -eq "0" -or $val -eq "1") -Detail $val
}

$utilCfg = $resolvedIncludePaths["DISCIPLINE_UTILITIES_CONFIG"]
$structCfg = $resolvedIncludePaths["DISCIPLINE_STRUCTURES_CONFIG"]
$geotechCfg = $resolvedIncludePaths["DISCIPLINE_GEOTECHNICAL_CONFIG"]
$plantCfg = $resolvedIncludePaths["DISCIPLINE_PLANT_CONFIG"]
$syncCfg = $resolvedIncludePaths["FUNCTION_SCHEDULING_CONFIG"]
$geoCfg = $resolvedIncludePaths["FUNCTION_GEOSPATIAL_CONFIG"]
$descCfg = $resolvedIncludePaths["FUNCTION_REALITY_DESCARTES_CONFIG"]
$itwinCfg = $resolvedIncludePaths["FUNCTION_REALITY_ITWIN_CAPTURE_CONFIG"]
$docCfg = $resolvedIncludePaths["FUNCTION_DOCUMENT_MANAGEMENT_CONFIG"]
$pythonCfg = $resolvedIncludePaths["FUNCTION_AUTOMATION_CONFIG"]

$exeChecks = @(
  @{ Tool = "FlowMaster"; Toggle = "INWC_INTEROP_OPENFLOWS"; Cfg = $utilCfg; Var = "FLOWMASTER_EXE" },
  @{ Tool = "WaterCAD"; Toggle = "INWC_INTEROP_OPENFLOWS"; Cfg = $utilCfg; Var = "WATERCAD_EXE" },
  @{ Tool = "WaterGEMS"; Toggle = "INWC_INTEROP_OPENFLOWS"; Cfg = $utilCfg; Var = "WATERGEMS_EXE" },
  @{ Tool = "HAMMER"; Toggle = "INWC_INTEROP_OPENFLOWS"; Cfg = $utilCfg; Var = "HAMMER_EXE" },
  @{ Tool = "SYNCHRO"; Toggle = "INWC_INTEROP_SYNCHRO"; Cfg = $syncCfg; Var = "SYNCHRO_EXE_PATH" },
  @{ Tool = "STAAD"; Toggle = "INWC_INTEROP_STRUCTURAL"; Cfg = $structCfg; Var = "STAAD_EXE" },
  @{ Tool = "AutoPIPE"; Toggle = "INWC_INTEROP_STRUCTURAL"; Cfg = $structCfg; Var = "AUTOPIPE_EXE" },
  @{ Tool = "RCDC"; Toggle = "INWC_INTEROP_STRUCTURAL"; Cfg = $structCfg; Var = "RCDC_EXE" },
  @{ Tool = "ADINA"; Toggle = "INWC_INTEROP_STRUCTURAL"; Cfg = $structCfg; Var = "ADINA_UI_EXE" },
  @{ Tool = "PLAXIS LE"; Toggle = "INWC_INTEROP_GEOTECH"; Cfg = $geotechCfg; Var = "PLAXIS_LE_EXE" },
  @{ Tool = "OpenPlant"; Toggle = "INWC_INTEROP_OPENPLANT"; Cfg = $plantCfg; Var = "OPENPLANT_EXE" },
  @{ Tool = "OpenPlant IsoExtractor"; Toggle = "INWC_INTEROP_OPENPLANT"; Cfg = $plantCfg; Var = "OPENPLANT_ISO_EXTRACTOR_EXE" },
  @{ Tool = "OpenCities Map"; Toggle = "INWC_INTEROP_OPENCITIES"; Cfg = $geoCfg; Var = "OPENCITIES_EXE" },
  @{ Tool = "Descartes"; Toggle = "INWC_INTEROP_DESCARTES"; Cfg = $descCfg; Var = "DESCARTES_EXE" },
  @{ Tool = "iTwin Capture"; Toggle = "INWC_INTEROP_ITWIN_CAPTURE"; Cfg = $itwinCfg; Var = "ITWIN_CAPTURE_EXE" },
  @{ Tool = "ProjectWise Drive"; Toggle = "INWC_INTEROP_PROJECTWISE_DRIVE"; Cfg = $docCfg; Var = "PROJECTWISE_DRIVE_EXE" },
  @{ Tool = "Bentley Python"; Toggle = "INWC_INTEROP_PYTHON_AUTOMATION"; Cfg = $pythonCfg; Var = "PYTHON_EXE" }
)

foreach ($item in $exeChecks) {
  if (-not $item.Cfg -or !(Test-Path -LiteralPath $item.Cfg)) {
    Add-Result -Bucket $results -Scope "Executable" -Check $item.Tool -Target $item.Cfg -OK $false -Detail "Config file missing"
    continue
  }

  $raw = Get-CfgVarValue -Path $item.Cfg -Var $item.Var
  $resolved = Resolve-CfgTokens -Value $raw -Tokens $tokens
  $exists = ($resolved -and (Test-Path -LiteralPath $resolved))

  $toggleValue = $toggleValues[$item.Toggle]
  $enabled = ($toggleValue -eq "1")

  $ok = if ($enabled) { $exists } else { $true }
  $detail = if ($enabled) { "Toggle=1" } else { "Toggle=0 (check informational)" }

  Add-Result -Bucket $results -Scope "Executable" -Check $item.Tool -Target $resolved -OK $ok -Detail ("{0}; Raw={1}" -f $detail, $raw)

  $installedTools.Add([pscustomobject]@{
    Tool = $item.Tool
    Toggle = $toggleValue
    Installed = $exists
    Path = $resolved
  }) | Out-Null
}

$folderChecks = @(
  @{ Path = (Join-Path $projectRoot "Analysis\Water"); Toggle = "INWC_INTEROP_OPENFLOWS" },
  @{ Path = (Join-Path $projectRoot "Reports\Hydraulic"); Toggle = "INWC_INTEROP_OPENFLOWS" },
  @{ Path = (Join-Path $projectRoot "Exports\SYNCHRO"); Toggle = "INWC_INTEROP_SYNCHRO" },
  @{ Path = (Join-Path $projectRoot "Analysis\Structural"); Toggle = "INWC_INTEROP_STRUCTURAL" },
  @{ Path = (Join-Path $projectRoot "Analysis\Geotechnical"); Toggle = "INWC_INTEROP_GEOTECH" },
  @{ Path = (Join-Path $projectRoot "Models\Plant"); Toggle = "INWC_INTEROP_OPENPLANT" },
  @{ Path = (Join-Path $projectRoot "Exports\OpenPlant\Isometrics"); Toggle = "INWC_INTEROP_OPENPLANT" },
  @{ Path = (Join-Path $projectRoot "Analysis\Geospatial"); Toggle = "INWC_INTEROP_OPENCITIES" },
  @{ Path = (Join-Path $projectRoot "Exports\OpenCities"); Toggle = "INWC_INTEROP_OPENCITIES" },
  @{ Path = (Join-Path $projectRoot "Analysis\Reality\RasterProcessed"); Toggle = "INWC_INTEROP_DESCARTES" },
  @{ Path = (Join-Path $projectRoot "Exports\iTwinCapture"); Toggle = "INWC_INTEROP_ITWIN_CAPTURE" },
  @{ Path = (Join-Path $projectRoot "DocumentManagement\ProjectWiseDrive"); Toggle = "INWC_INTEROP_PROJECTWISE_DRIVE" },
  @{ Path = (Join-Path $cfgRoot "Automation\Python"); Toggle = "INWC_INTEROP_PYTHON_AUTOMATION" }
)

foreach ($item in $folderChecks) {
  $toggleValue = $toggleValues[$item.Toggle]
  $enabled = ($toggleValue -eq "1")
  $exists = Test-Path -LiteralPath $item.Path

  $ok = if ($enabled) { $exists } else { $true }
  $detail = if ($enabled) { "Toggle=1" } else { "Toggle=0 (check informational)" }
  Add-Result -Bucket $results -Scope "Folder" -Check "Path exists" -Target $item.Path -OK $ok -Detail $detail
}

# ProjectWise local-drive consistency
$pwEnabledSetting = Get-CfgVarValue -Path $orgCfg -Var "PROJECTWISE_ENABLED"
$pwServerModeOk = ($pwEnabledSetting -ne "1")
Add-Result -Bucket $results -Scope "Policy" -Check "ProjectWise server mode not forced" -Target $orgCfg -OK $pwServerModeOk -Detail ("PROJECTWISE_ENABLED={0}" -f $pwEnabledSetting)

if ($docCfg -and (Test-Path -LiteralPath $docCfg)) {
  $pwDriveEnabled = Get-CfgVarValue -Path $docCfg -Var "PROJECTWISE_DRIVE_ENABLED"
  Add-Result -Bucket $results -Scope "Policy" -Check "PROJECTWISE_DRIVE_ENABLED uses interop toggle" -Target $docCfg -OK ($pwDriveEnabled -eq '$(INWC_INTEROP_PROJECTWISE_DRIVE)') -Detail $pwDriveEnabled
}

$results | Format-Table -AutoSize

Write-Host ""
Write-Host "Installed Tool Snapshot:" -ForegroundColor Cyan
$installedTools | Sort-Object Tool | Format-Table -AutoSize

if ($results.OK -contains $false) {
  Write-Host "`nFAIL: integration checks found issues." -ForegroundColor Red
  exit 1
}

Write-Host "`nPASS: integration checks passed." -ForegroundColor Green
exit 0

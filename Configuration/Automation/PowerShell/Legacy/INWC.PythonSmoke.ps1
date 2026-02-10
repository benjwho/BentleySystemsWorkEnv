#Requires -Version 5.1
[CmdletBinding()]
param(
  [string]$TechRoot = "C:\Users\zohar\Documents\INWC_RH\tech",
  [switch]$SkipRuntimeTest
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
    if ($line -match $rx) { return $Matches[1].Trim() }
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
  }) | Out-Null
}

$cfgRoot = Join-Path $TechRoot "Configuration"
$projectRoot = Join-Path $TechRoot "Projects\NWC_Rehab"
$logRoot = Join-Path $TechRoot "Logs\NWC_Rehab"
if (!(Test-Path $logRoot)) {
  New-Item -Path $logRoot -ItemType Directory -Force | Out-Null
}

$tokens = @{
  "INWC_TECH" = $TechRoot
  "INWC_CFG" = $cfgRoot
  "INWC_PROJECT_ROOT" = $projectRoot
  "INWC_EXPORT" = (Join-Path $projectRoot "Exports")
  "INWC_DATA" = (Join-Path $TechRoot "Data")
}

$orgCfg = Join-Path $cfgRoot "Organization\ByProject\NWC_Rehab\NWC_Rehab_Organization.cfg"
$interopCfg = Join-Path $cfgRoot "Organization\Interoperability\Interoperability.cfg"
$pythonCfg = Join-Path $cfgRoot "Organization\ByFunction\Automation\Python_Integration.cfg"

$results = New-Object System.Collections.Generic.List[object]

# A) Config presence
foreach ($f in @($orgCfg, $interopCfg, $pythonCfg)) {
  Add-Result -Scope "Config" -Check "File exists" -Target $f -OK (Test-Path $f)
}

# B) Include chain and toggle wiring
$automationVarRaw = Get-CfgVarValue -Path $orgCfg -Var "FUNCTION_AUTOMATION_CONFIG"
$automationVarResolved = if ($automationVarRaw) { Resolve-CfgTokens -Value $automationVarRaw -Tokens $tokens } else { $null }
$automationVarOk = ($automationVarResolved -and (Test-Path $automationVarResolved))
Add-Result -Scope "Config" -Check "FUNCTION_AUTOMATION_CONFIG resolves" -Target $automationVarResolved -OK $automationVarOk -Detail $automationVarRaw

$orgText = if (Test-Path $orgCfg) { Get-Content -Path $orgCfg -Raw } else { "" }
$hasAutomationInclude = ($orgText -match '(?m)^\s*%include\s+\$\(FUNCTION_AUTOMATION_CONFIG\)\s*$')
Add-Result -Scope "Config" -Check '%include $(FUNCTION_AUTOMATION_CONFIG) present' -Target $orgCfg -OK $hasAutomationInclude

$toggleRaw = Get-CfgVarValue -Path $interopCfg -Var "INWC_INTEROP_PYTHON_AUTOMATION"
$toggleOk = ($toggleRaw -eq "0" -or $toggleRaw -eq "1")
Add-Result -Scope "Toggle" -Check "INWC_INTEROP_PYTHON_AUTOMATION is 0/1" -Target $interopCfg -OK $toggleOk -Detail $toggleRaw

$enabledRaw = Get-CfgVarValue -Path $pythonCfg -Var "PYTHON_AUTOMATION_ENABLED"
$enabledUsesToggle = ($enabledRaw -eq '$(INWC_INTEROP_PYTHON_AUTOMATION)')
Add-Result -Scope "Config" -Check "PYTHON_AUTOMATION_ENABLED uses interoperability toggle" -Target $pythonCfg -OK $enabledUsesToggle -Detail $enabledRaw

# C) Path checks
$pythonExeRaw = Get-CfgVarValue -Path $pythonCfg -Var "PYTHON_EXE"
$pythonExe = if ($pythonExeRaw) { Resolve-CfgTokens -Value $pythonExeRaw -Tokens $tokens } else { $null }
$pythonExeOk = ($pythonExe -and (Test-Path $pythonExe))
Add-Result -Scope "Executable" -Check "PYTHON_EXE exists" -Target $pythonExe -OK $pythonExeOk -Detail $pythonExeRaw

$scriptsDirRaw = Get-CfgVarValue -Path $pythonCfg -Var "PYTHON_SCRIPTS_DIR"
$scriptsDir = if ($scriptsDirRaw) { Resolve-CfgTokens -Value $scriptsDirRaw -Tokens $tokens } else { $null }
$scriptsDirOk = ($scriptsDir -and (Test-Path $scriptsDir))
Add-Result -Scope "Folders" -Check "PYTHON_SCRIPTS_DIR exists" -Target $scriptsDir -OK $scriptsDirOk -Detail $scriptsDirRaw

$pythonLogDirRaw = Get-CfgVarValue -Path $pythonCfg -Var "PYTHON_LOG_DIR"
$pythonLogDir = if ($pythonLogDirRaw) { Resolve-CfgTokens -Value $pythonLogDirRaw -Tokens $tokens } else { $null }
$pythonLogDirOk = ($pythonLogDir -and (Test-Path $pythonLogDir))
Add-Result -Scope "Folders" -Check "PYTHON_LOG_DIR exists" -Target $pythonLogDir -OK $pythonLogDirOk -Detail $pythonLogDirRaw

# D) Write test in PYTHON_LOG_DIR
if ($pythonLogDirOk) {
  $tmp = Join-Path $pythonLogDir ("._inwc_python_smoke_{0}.tmp" -f ([guid]::NewGuid().ToString("N")))
  try {
    Set-Content -Path $tmp -Value "INWC Python smoke write test" -Encoding ascii
    $wrote = Test-Path $tmp
    if ($wrote) {
      Remove-Item -Path $tmp -Force -ErrorAction Stop
    }
    Add-Result -Scope "WriteTest" -Check "PYTHON_LOG_DIR writable" -Target $pythonLogDir -OK $wrote
  } catch {
    Add-Result -Scope "WriteTest" -Check "PYTHON_LOG_DIR writable" -Target $pythonLogDir -OK $false -Detail $_.Exception.Message
    if (Test-Path $tmp) {
      Remove-Item -Path $tmp -Force -ErrorAction SilentlyContinue
    }
  }
} else {
  Add-Result -Scope "WriteTest" -Check "PYTHON_LOG_DIR writable" -Target $pythonLogDir -OK $false -Detail "PYTHON_LOG_DIR missing"
}

# E) Runtime smoke
if ($SkipRuntimeTest) {
  Add-Result -Scope "Runtime" -Check "Python runtime smoke" -Target $pythonExe -OK $true -Detail "Skipped by switch"
} elseif (-not $pythonExeOk) {
  Add-Result -Scope "Runtime" -Check "Python runtime smoke" -Target $pythonExe -OK $false -Detail "PYTHON_EXE missing"
} else {
  $versionOut = & $pythonExe --version 2>&1
  $versionExit = $LASTEXITCODE
  Add-Result -Scope "Runtime" -Check "python --version exits 0" -Target $pythonExe -OK ($versionExit -eq 0) -Detail (($versionOut | Out-String).Trim())

  $smokeOut = & $pythonExe -c "import platform; print('INWC_PYTHON_SMOKE_OK|' + platform.python_version())" 2>&1
  $smokeExit = $LASTEXITCODE
  $smokeText = ($smokeOut | Out-String).Trim()
  $smokeOk = ($smokeExit -eq 0 -and $smokeText -match 'INWC_PYTHON_SMOKE_OK\|')
  Add-Result -Scope "Runtime" -Check "Inline Python script executes" -Target $pythonExe -OK $smokeOk -Detail $smokeText
}

$results | Format-Table -AutoSize

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss-fff"
$logPath = Join-Path $logRoot ("PythonSmoke_{0}.txt" -f $timestamp)
$logLines = @()
$logLines += "INWC Python smoke check"
$logLines += "timestamp: $((Get-Date).ToString('s'))"
$logLines += "tech_root: $TechRoot"
$logLines += "skip_runtime_test: $SkipRuntimeTest"
$logLines += ""
$logLines += ($results | Format-Table -AutoSize | Out-String).TrimEnd()
$logLines | Set-Content -Path $logPath -Encoding ascii

$failed = @($results | Where-Object { -not $_.OK }).Count
if ($failed -gt 0) {
  Write-Host ""
  Write-Host ("FAIL: Python smoke checks found {0} issue(s)." -f $failed) -ForegroundColor Red
  Write-Host ("Log: {0}" -f $logPath)
  exit 1
}

Write-Host ""
Write-Host "PASS: Python smoke checks passed." -ForegroundColor Green
Write-Host ("Log: {0}" -f $logPath)
exit 0


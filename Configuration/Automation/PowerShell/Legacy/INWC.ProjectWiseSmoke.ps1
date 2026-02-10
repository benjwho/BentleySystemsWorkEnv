#Requires -Version 5.1
[CmdletBinding()]
param(
  [string]$TechRoot = "C:\Users\zohar\Documents\INWC_RH\tech",
  [switch]$SkipWriteTest
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
  })
}

$cfgRoot = Join-Path $TechRoot "Configuration"
$projectRoot = Join-Path $TechRoot "Projects\NWC_Rehab"
$logRoot = Join-Path $TechRoot "Logs\NWC_Rehab"

$tokens = @{
  "INWC_TECH" = $TechRoot
  "INWC_CFG" = $cfgRoot
  "INWC_PROJECT_ROOT" = $projectRoot
  "INWC_EXPORT" = (Join-Path $projectRoot "Exports")
  "INWC_DATA" = (Join-Path $TechRoot "Data")
}

$orgCfg = Join-Path $cfgRoot "Organization\ByProject\NWC_Rehab\NWC_Rehab_Organization.cfg"
$interopCfg = Join-Path $cfgRoot "Organization\Interoperability\Interoperability.cfg"
$pwCfg = Join-Path $cfgRoot "Organization\ByFunction\DocumentManagement\ProjectWiseDrive_Integration.cfg"

if (!(Test-Path $logRoot)) {
  New-Item -Path $logRoot -ItemType Directory -Force | Out-Null
}

$results = New-Object System.Collections.Generic.List[object]

# A) Base config presence
foreach ($f in @($orgCfg, $interopCfg, $pwCfg)) {
  Add-Result -Scope "Config" -Check "File exists" -Target $f -OK (Test-Path $f)
}

# B) Include chain and toggle
$docVarRaw = Get-CfgVarValue -Path $orgCfg -Var "FUNCTION_DOCUMENT_MANAGEMENT_CONFIG"
$docVarResolved = if ($docVarRaw) { Resolve-CfgTokens -Value $docVarRaw -Tokens $tokens } else { $null }
$docVarOk = ($docVarResolved -and (Test-Path $docVarResolved))
Add-Result -Scope "Config" -Check "FUNCTION_DOCUMENT_MANAGEMENT_CONFIG resolves" -Target $docVarResolved -OK $docVarOk -Detail $docVarRaw

$orgText = if (Test-Path $orgCfg) { Get-Content -Path $orgCfg -Raw } else { "" }
$hasDocInclude = ($orgText -match '(?m)^\s*%include\s+\$\(FUNCTION_DOCUMENT_MANAGEMENT_CONFIG\)\s*$')
Add-Result -Scope "Config" -Check '%include $(FUNCTION_DOCUMENT_MANAGEMENT_CONFIG) present' -Target $orgCfg -OK $hasDocInclude

$interopRaw = Get-CfgVarValue -Path $interopCfg -Var "INWC_INTEROP_PROJECTWISE_DRIVE"
$interopToggleOk = ($interopRaw -eq "0" -or $interopRaw -eq "1")
Add-Result -Scope "Toggle" -Check "INWC_INTEROP_PROJECTWISE_DRIVE is 0/1" -Target $interopCfg -OK $interopToggleOk -Detail $interopRaw

$pwEnabledRaw = Get-CfgVarValue -Path $pwCfg -Var "PROJECTWISE_DRIVE_ENABLED"
$pwEnabledUsesToggle = ($pwEnabledRaw -eq '$(INWC_INTEROP_PROJECTWISE_DRIVE)')
Add-Result -Scope "Config" -Check "PROJECTWISE_DRIVE_ENABLED uses interoperability toggle" -Target $pwCfg -OK $pwEnabledUsesToggle -Detail $pwEnabledRaw

# C) Executable + path checks
$pwExeRaw = Get-CfgVarValue -Path $pwCfg -Var "PROJECTWISE_DRIVE_EXE"
$pwExeResolved = if ($pwExeRaw) { Resolve-CfgTokens -Value $pwExeRaw -Tokens $tokens } else { $null }
$pwExeOk = ($pwExeResolved -and (Test-Path $pwExeResolved))
Add-Result -Scope "Executable" -Check "PROJECTWISE_DRIVE_EXE exists" -Target $pwExeResolved -OK $pwExeOk -Detail $pwExeRaw

$dirVars = @(
  "PROJECTWISE_DRIVE_WORK_DIR",
  "PROJECTWISE_DRIVE_SYNC_DIR",
  "PROJECTWISE_DRIVE_CACHE_DIR"
)

$resolvedDirs = @()
foreach ($v in $dirVars) {
  $raw = Get-CfgVarValue -Path $pwCfg -Var $v
  $resolved = if ($raw) { Resolve-CfgTokens -Value $raw -Tokens $tokens } else { $null }
  $ok = ($resolved -and (Test-Path $resolved))
  Add-Result -Scope "Folders" -Check "$v exists" -Target $resolved -OK $ok -Detail $raw
  if ($resolved) { $resolvedDirs += $resolved }
}

# D) Safe write-access checks
if ($SkipWriteTest) {
  foreach ($d in $resolvedDirs) {
    Add-Result -Scope "WriteTest" -Check "Skipped write test" -Target $d -OK $true -Detail "SkipWriteTest switch provided"
  }
} else {
  foreach ($d in $resolvedDirs) {
    if (!(Test-Path $d)) {
      Add-Result -Scope "WriteTest" -Check "Directory writable" -Target $d -OK $false -Detail "Directory missing"
      continue
    }

    $tmp = Join-Path $d ("._inwc_pw_smoke_{0}.tmp" -f ([guid]::NewGuid().ToString("N")))
    try {
      Set-Content -Path $tmp -Value "INWC ProjectWise smoke write test" -Encoding ascii
      $wrote = Test-Path $tmp
      if ($wrote) {
        Remove-Item -Path $tmp -Force -ErrorAction Stop
      }
      Add-Result -Scope "WriteTest" -Check "Directory writable" -Target $d -OK $wrote
    } catch {
      Add-Result -Scope "WriteTest" -Check "Directory writable" -Target $d -OK $false -Detail $_.Exception.Message
      if (Test-Path $tmp) {
        Remove-Item -Path $tmp -Force -ErrorAction SilentlyContinue
      }
    }
  }
}

# E) Optional runtime info
$pwProc = Get-Process -Name "ProjectWise Drive" -ErrorAction SilentlyContinue
$isRunning = ($null -ne $pwProc)
Add-Result -Scope "Runtime" -Check "ProjectWise Drive process running (informational)" -Target "ProjectWise Drive" -OK $true -Detail ("Running={0}" -f $isRunning)

$results | Format-Table -AutoSize

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss-fff"
$logPath = Join-Path $logRoot ("ProjectWiseSmoke_{0}.txt" -f $timestamp)
$logLines = @()
$logLines += "INWC ProjectWise smoke check"
$logLines += "timestamp: $((Get-Date).ToString('s'))"
$logLines += "tech_root: $TechRoot"
$logLines += "skip_write_test: $SkipWriteTest"
$logLines += ""
$logLines += ($results | Format-Table -AutoSize | Out-String).TrimEnd()
$logLines | Set-Content -Path $logPath -Encoding ascii

$failed = @($results | Where-Object { -not $_.OK }).Count
if ($failed -gt 0) {
  Write-Host ""
  Write-Host ("FAIL: ProjectWise smoke checks found {0} issue(s)." -f $failed) -ForegroundColor Red
  Write-Host ("Log: {0}" -f $logPath)
  exit 1
}

Write-Host ""
Write-Host "PASS: ProjectWise smoke checks passed." -ForegroundColor Green
Write-Host ("Log: {0}" -f $logPath)
exit 0

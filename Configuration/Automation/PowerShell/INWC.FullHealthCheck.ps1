#Requires -Version 5.1
[CmdletBinding()]
param(
  [string]$TechRoot = "C:\Users\zohar\Documents\INWC_RH\tech",
  [switch]$SkipPythonRuntimeTest,
  [switch]$SkipProjectWiseWriteTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Join-Path $TechRoot "Configuration\Automation\PowerShell"
$logRoot = Join-Path $TechRoot "Logs\NWC_Rehab"
if (!(Test-Path $logRoot)) {
  New-Item -Path $logRoot -ItemType Directory -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss-fff"
$logPath = Join-Path $logRoot ("FullHealthCheck_{0}.txt" -f $timestamp)

function Write-Log {
  param([string]$Text)
  Write-Host $Text
  Add-Content -Path $logPath -Value $Text -Encoding ascii
}

function Invoke-ChildCheck {
  param(
    [Parameter(Mandatory)] [string]$Name,
    [Parameter(Mandatory)] [string]$ScriptPath,
    [string[]]$ExtraArgs = @()
  )

  $pwshExe = (Get-Command powershell.exe).Source
  $args = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $ScriptPath, "-TechRoot", $TechRoot) + $ExtraArgs

  Write-Log ""
  Write-Log ("=== {0} ===" -f $Name)
  Write-Log ("Script: {0}" -f $ScriptPath)

  $output = & $pwshExe @args 2>&1
  $exitCode = $LASTEXITCODE

  foreach ($line in $output) {
    Write-Log ($line.ToString())
  }

  $ok = ($exitCode -eq 0)
  Write-Log ("ExitCode: {0}" -f $exitCode)
  return [pscustomobject]@{
    Check = $Name
    Script = $ScriptPath
    ExitCode = $exitCode
    OK = $ok
  }
}

$checks = @(
  [pscustomobject]@{
    Name = "Environment"
    Script = (Join-Path $scriptRoot "INWC.EnvCheck.ps1")
    Args = @()
  },
  [pscustomobject]@{
    Name = "Integration"
    Script = (Join-Path $scriptRoot "INWC.IntegrationCheck.ps1")
    Args = @()
  },
  [pscustomobject]@{
    Name = "Python Smoke"
    Script = (Join-Path $scriptRoot "INWC.PythonSmoke.ps1")
    Args = $(if ($SkipPythonRuntimeTest) { @("-SkipRuntimeTest") } else { @() })
  },
  [pscustomobject]@{
    Name = "ProjectWise Smoke"
    Script = (Join-Path $scriptRoot "INWC.ProjectWiseSmoke.ps1")
    Args = $(if ($SkipProjectWiseWriteTest) { @("-SkipWriteTest") } else { @() })
  }
)

Write-Log "INWC Full Health Check"
Write-Log ("Timestamp: {0}" -f (Get-Date).ToString("s"))
Write-Log ("TechRoot: {0}" -f $TechRoot)
Write-Log ("SkipPythonRuntimeTest: {0}" -f $SkipPythonRuntimeTest)
Write-Log ("SkipProjectWiseWriteTest: {0}" -f $SkipProjectWiseWriteTest)

$results = New-Object System.Collections.Generic.List[object]
foreach ($c in $checks) {
  if (!(Test-Path $c.Script)) {
    Write-Log ""
    Write-Log ("=== {0} ===" -f $c.Name)
    Write-Log ("Script missing: {0}" -f $c.Script)
    $results.Add([pscustomobject]@{
      Check = $c.Name
      Script = $c.Script
      ExitCode = 127
      OK = $false
    }) | Out-Null
    continue
  }
  $results.Add((Invoke-ChildCheck -Name $c.Name -ScriptPath $c.Script -ExtraArgs $c.Args)) | Out-Null
}

Write-Log ""
Write-Log "=== Summary ==="
$summary = $results | Select-Object Check, ExitCode, OK
$summaryText = ($summary | Format-Table -AutoSize | Out-String).TrimEnd()
Write-Log $summaryText

$failed = @($results | Where-Object { -not $_.OK }).Count
Write-Log ""
Write-Log ("Log: {0}" -f $logPath)

if ($failed -gt 0) {
  Write-Log ("FAIL: {0} check(s) failed." -f $failed)
  exit 1
}

Write-Log "PASS: all checks passed."
exit 0

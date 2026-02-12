#Requires -Version 5.1
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
  [string]$TechRoot = "C:\Users\zohar\Documents\INWC_RH\tech",
  [int]$WaitSeconds = 25,
  [int]$UserActionSeconds = 20,
  [switch]$AutoPilot,
  [string]$AutoPilotFilePath,
  [switch]$AutoPilotUseSendKeys,
  [switch]$SkipLaunch,
  [switch]$KeepAppsOpen,
  [switch]$PickerFailed,
  [switch]$WaitForEnterBeforeCapture,
  [switch]$NonInteractive
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Join-Path $TechRoot "Automation\PowerShell"
$logRoot = Join-Path $TechRoot "Logs\NWC_Rehab"
if (!(Test-Path -LiteralPath $logRoot)) {
  New-Item -Path $logRoot -ItemType Directory -Force | Out-Null
}

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$auditRoot = Join-Path $logRoot ("PickerAudit_{0}" -f $stamp)
$logPath = Join-Path $auditRoot "PickerAudit.log"
New-Item -Path $auditRoot -ItemType Directory -Force | Out-Null

$productConfigRoots = @(
  "C:\ProgramData\Bentley\MicroStation 2025\Configuration",
  "C:\ProgramData\Bentley\OpenRoads Designer 2025.00\Configuration",
  "C:\ProgramData\Bentley\OpenSite Designer 2025.00\Configuration",
  "C:\ProgramData\Bentley\OpenCities Map Ultimate 2025\Configuration",
  "C:\ProgramData\Bentley\Bentley Descartes 2025\Configuration",
  "C:\ProgramData\Bentley\OpenPlant 2024\Configuration"
)

function Write-Log {
  param([string]$Text)
  $line = "[{0}] {1}" -f (Get-Date -Format "s"), $Text
  Write-Host $line
  Add-Content -LiteralPath $logPath -Value $line -Encoding ASCII
}

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

function Resolve-Executable {
  param(
    [Parameter(Mandatory)] [string]$ProductName,
    [Parameter(Mandatory)] [string[]]$Candidates,
    [Parameter(Mandatory)] [string[]]$NamePatterns
  )

  foreach ($candidate in $Candidates) {
    if (Test-Path -LiteralPath $candidate) {
      return $candidate
    }
  }

  $searchRoots = @(
    "C:\Program Files\Bentley",
    "C:\Program Files (x86)\Bentley"
  )

  foreach ($root in $searchRoots) {
    if (!(Test-Path -LiteralPath $root)) { continue }
    foreach ($pattern in $NamePatterns) {
      $found = Get-ChildItem -Path $root -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue |
        Select-Object -First 1 -ExpandProperty FullName
      if ($found) { return $found }
    }
  }

  throw "Could not find executable for $ProductName"
}

function Save-DesktopScreenshot {
  param([Parameter(Mandatory)] [string]$Path)

  Add-Type -AssemblyName System.Windows.Forms
  Add-Type -AssemblyName System.Drawing

  $bounds = [System.Windows.Forms.SystemInformation]::VirtualScreen
  $bitmap = New-Object System.Drawing.Bitmap($bounds.Width, $bounds.Height)
  $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
  try {
    $graphics.CopyFromScreen($bounds.Left, $bounds.Top, 0, 0, $bitmap.Size)
    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
  } finally {
    $graphics.Dispose()
    $bitmap.Dispose()
  }
}

function Escape-SendKeysLiteral {
  param([Parameter(Mandatory)] [string]$Value)

  $escaped = $Value
  foreach ($ch in @('{', '}', '(', ')', '+', '^', '%', '~', '[', ']')) {
    $escaped = $escaped.Replace($ch, ("{" + $ch + "}"))
  }
  return $escaped
}

function Focus-ProcessWindow {
  param(
    [Parameter(Mandatory)] [System.Diagnostics.Process]$Process,
    [int]$TimeoutSeconds = 30
  )

  Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public static class Win32Focus {
  [DllImport("user32.dll")]
  public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")]
  public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
}
"@ -ErrorAction SilentlyContinue

  $deadline = (Get-Date).AddSeconds([Math]::Max(1, $TimeoutSeconds))
  while ((Get-Date) -lt $deadline) {
    if ($Process.HasExited) { return $false }

    $Process.Refresh()
    $hWnd = $Process.MainWindowHandle
    if ($hWnd -and $hWnd -ne [IntPtr]::Zero) {
      [void][Win32Focus]::ShowWindowAsync($hWnd, 9)
      [void][Win32Focus]::SetForegroundWindow($hWnd)
      Start-Sleep -Milliseconds 500
      return $true
    }

    Start-Sleep -Milliseconds 500
  }

  return $false
}

function Activate-ProcessWindow {
  param(
    [Parameter(Mandatory)] [System.Diagnostics.Process]$Process,
    [int]$TimeoutSeconds = 10
  )

  $shell = New-Object -ComObject WScript.Shell
  $deadline = (Get-Date).AddSeconds([Math]::Max(1, $TimeoutSeconds))
  while ((Get-Date) -lt $deadline) {
    if ($Process.HasExited) { return $false }
    if ($shell.AppActivate($Process.Id)) {
      Start-Sleep -Milliseconds 300
      return $true
    }
    Start-Sleep -Milliseconds 300
  }
  return $false
}

function Try-AutoPilotOpenFile {
  param(
    [Parameter(Mandatory)] [System.Diagnostics.Process]$Process,
    [Parameter(Mandatory)] [string]$ProductName,
    [Parameter(Mandatory)] [string]$TargetFilePath
  )

  if (!(Test-Path -LiteralPath $TargetFilePath)) {
    Write-Log ("WARN: AutoPilot file missing for {0}: {1}" -f $ProductName, $TargetFilePath)
    return $false
  }

  Add-Type -AssemblyName System.Windows.Forms

  if (-not (Activate-ProcessWindow -Process $Process -TimeoutSeconds 8)) {
    Write-Log ("WARN: AutoPilot could not activate {0} window." -f $ProductName)
    return $false
  }

  [void](Focus-ProcessWindow -Process $Process -TimeoutSeconds 2)
  Start-Sleep -Milliseconds 500

  $sendPath = Escape-SendKeysLiteral -Value $TargetFilePath

  # Best-effort sequence for Bentley/Windows Open dialogs:
  # open dialog, focus filename field, inject full path, submit.
  [System.Windows.Forms.SendKeys]::SendWait("^o")
  Start-Sleep -Milliseconds 800
  [System.Windows.Forms.SendKeys]::SendWait("%n")
  Start-Sleep -Milliseconds 350
  [System.Windows.Forms.SendKeys]::SendWait("^a")
  Start-Sleep -Milliseconds 120
  [System.Windows.Forms.SendKeys]::SendWait("{DEL}")
  Start-Sleep -Milliseconds 120
  [System.Windows.Forms.SendKeys]::SendWait($sendPath)
  Start-Sleep -Milliseconds 200
  [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
  Start-Sleep -Seconds 4

  Write-Log ("AutoPilot attempted file open in {0}: {1}" -f $ProductName, $TargetFilePath)
  return $true
}

function Try-CloseProcess {
  param([Parameter(Mandatory)] [System.Diagnostics.Process]$Process)

  if ($Process.HasExited) { return }

  try {
    [void]$Process.CloseMainWindow()
    Start-Sleep -Seconds 5
    if (-not $Process.HasExited) {
      Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
    }
  } catch {
    Write-Log ("WARN: unable to close process {0} ({1}) cleanly: {2}" -f $Process.ProcessName, $Process.Id, $_.Exception.Message)
  }
}

function Invoke-AppPickerCapture {
  param(
    [Parameter(Mandatory)] [string]$Name,
    [Parameter(Mandatory)] [string]$ExecutablePath,
    [Parameter(Mandatory)] [string]$TargetFilePath
  )

  Write-Log ("Launching {0}: {1}" -f $Name, $ExecutablePath)

  if (-not $PSCmdlet.ShouldProcess($ExecutablePath, "Launch $Name and capture picker screenshot")) {
    return $null
  }

  $launchArgumentList = $null
  if ($AutoPilot -and -not $AutoPilotUseSendKeys -and (Test-Path -LiteralPath $TargetFilePath)) {
    $launchArgumentList = @($TargetFilePath)
    Write-Log ("AutoPilot launch-arg mode for {0}: {1}" -f $Name, $TargetFilePath)
  } elseif ($AutoPilot -and -not (Test-Path -LiteralPath $TargetFilePath)) {
    Write-Log ("WARN: AutoPilot target file not found for {0}: {1}" -f $Name, $TargetFilePath)
  }

  if ($launchArgumentList) {
    $proc = Start-Process -FilePath $ExecutablePath -ArgumentList $launchArgumentList -PassThru
  } else {
    $proc = Start-Process -FilePath $ExecutablePath -PassThru
  }
  Write-Log ("Started {0} PID={1}" -f $Name, $proc.Id)

  $launchTime = Get-Date
  $focused = Focus-ProcessWindow -Process $proc -TimeoutSeconds ([Math]::Min(30, [Math]::Max(5, $WaitSeconds)))
  if ($focused) {
    Write-Log ("Focused window for {0} before screenshot." -f $Name)
  } else {
    Write-Log ("WARN: could not focus {0} window before screenshot." -f $Name)
  }

  $elapsed = ((Get-Date) - $launchTime).TotalSeconds
  $remaining = [int][Math]::Ceiling($WaitSeconds - $elapsed)
  if ($remaining -gt 0) {
    Write-Log ("Waiting {0}s for {1} to finish loading." -f $remaining, $Name)
    Start-Sleep -Seconds $remaining
  }

  if ($AutoPilot) {
    if ($AutoPilotUseSendKeys) {
      Write-Log ("AutoPilot SendKeys fallback enabled for {0}." -f $Name)
      [void](Try-AutoPilotOpenFile -Process $proc -ProductName $Name -TargetFilePath $TargetFilePath)
    } else {
      Write-Log ("AutoPilot using launch arguments only for {0}; no SendKeys injection." -f $Name)
    }
  } else {
    if ($WaitForEnterBeforeCapture -and -not $NonInteractive) {
      [void](Read-Host ("Set workspace/workset and open a file in {0}, then press Enter to capture screenshot" -f $Name))
    } elseif ($UserActionSeconds -gt 0) {
      Write-Log ("Manual prep window: {0}s for {1} (set workspace/workset and open file)." -f $UserActionSeconds, $Name)
      Start-Sleep -Seconds $UserActionSeconds
    }
  }

  [void](Focus-ProcessWindow -Process $proc -TimeoutSeconds 3)
  $screenPath = Join-Path $auditRoot ("{0}_Picker_{1}.png" -f ($Name -replace "\s+", "_"), (Get-Date -Format "HHmmss"))
  Save-DesktopScreenshot -Path $screenPath
  Write-Log ("Screenshot saved: {0}" -f $screenPath)

  if (-not $KeepAppsOpen) {
    Try-CloseProcess -Process $proc
    Write-Log ("Closed {0} (if still running)." -f $Name)
  } else {
    Write-Log ("Left {0} running due to -KeepAppsOpen." -f $Name)
  }

  return $screenPath
}

function Export-EnvCheckSnapshot {
  $envCheckPath = Join-Path $scriptRoot "INWC.EnvCheck.ps1"
  if (!(Test-Path -LiteralPath $envCheckPath)) {
    Write-Log "WARN: INWC.EnvCheck.ps1 missing; skipping env snapshot."
    return $null
  }

  $snapshotPath = Join-Path $auditRoot "EnvCheck.txt"
  $pwshExe = (Get-Command powershell.exe).Source
  $output = & $pwshExe -NoProfile -ExecutionPolicy Bypass -File $envCheckPath -TechRoot $TechRoot 2>&1
  $exitCode = $LASTEXITCODE
  $output | Out-File -LiteralPath $snapshotPath -Encoding ASCII
  Write-Log ("EnvCheck snapshot written: {0} (ExitCode={1})" -f $snapshotPath, $exitCode)
  return $snapshotPath
}

function Export-ConfigResolutionDiagnostics {
  $diagRoot = Join-Path $auditRoot "Diagnostics"
  New-Item -Path $diagRoot -ItemType Directory -Force | Out-Null

  $cfgRootWin = Join-Path $TechRoot "Configuration"
  $expectedCfgRoot = Normalize-CfgRoot $cfgRootWin
  $userEnv = Normalize-CfgRoot ([Environment]::GetEnvironmentVariable("_USTN_CUSTOM_CONFIGURATION", "User"))
  $machineEnv = Normalize-CfgRoot ([Environment]::GetEnvironmentVariable("_USTN_CUSTOM_CONFIGURATION", "Machine"))
  $fallbackEnv = if ($userEnv) { $userEnv } else { $machineEnv }

  $summary = New-Object System.Collections.Generic.List[object]
  foreach ($productRoot in $productConfigRoots) {
    $setupCfg = Join-Path $productRoot "ConfigurationSetup.cfg"
    if (!(Test-Path -LiteralPath $setupCfg)) {
      $summary.Add([pscustomobject]@{
        ProductRoot = $productRoot
        ConfigSetup = $setupCfg
        Exists = $false
        RawCustom = $null
        NormalizedCustom = $null
        EffectiveCustom = $fallbackEnv
        IncludePath = $null
        IncludeResolves = $false
      }) | Out-Null
      continue
    }

    $safeName = ($productRoot -replace "[:\\ ]", "_").Trim("_")
    Copy-Item -LiteralPath $setupCfg -Destination (Join-Path $diagRoot ("{0}_ConfigurationSetup.cfg" -f $safeName)) -Force

    $rawCustom = Get-CfgVarValue -Path $setupCfg -Var "_USTN_CUSTOM_CONFIGURATION"
    $normalizedCustom = Normalize-CfgRoot $rawCustom
    $effectiveCustom = if ($normalizedCustom) { $normalizedCustom } else { $fallbackEnv }

    $includePath = if ($effectiveCustom) {
      (($effectiveCustom -replace "/", "\") + "WorkSpaceSetup.cfg")
    } else {
      $null
    }

    $summary.Add([pscustomobject]@{
      ProductRoot = $productRoot
      ConfigSetup = $setupCfg
      Exists = $true
      RawCustom = $rawCustom
      NormalizedCustom = $normalizedCustom
      EffectiveCustom = $effectiveCustom
      IncludePath = $includePath
      IncludeResolves = [bool]($includePath -and (Test-Path -LiteralPath $includePath))
    }) | Out-Null
  }

  $summaryPath = Join-Path $diagRoot "ConfigResolutionSummary.txt"
  $summaryCsvPath = Join-Path $diagRoot "ConfigResolutionSummary.csv"
  $summaryJsonPath = Join-Path $diagRoot "ConfigResolutionSummary.json"
  $header = @(
    ("Timestamp: {0}" -f (Get-Date -Format "s"))
    ("TechRoot: {0}" -f $TechRoot)
    ("Expected INWC cfg root: {0}" -f $expectedCfgRoot)
    ("User _USTN_CUSTOM_CONFIGURATION: {0}" -f $userEnv)
    ("Machine _USTN_CUSTOM_CONFIGURATION: {0}" -f $machineEnv)
    ""
    "Per-product _USTN_CUSTOM_CONFIGURATION resolution:"
    ""
  )
  $header | Out-File -LiteralPath $summaryPath -Encoding ASCII
  ($summary | Format-Table -AutoSize | Out-String).TrimEnd() | Out-File -LiteralPath $summaryPath -Encoding ASCII -Append
  $summary | Export-Csv -LiteralPath $summaryCsvPath -NoTypeInformation -Encoding ASCII
  $summary | ConvertTo-Json -Depth 4 | Out-File -LiteralPath $summaryJsonPath -Encoding ASCII

  Write-Log ("Diagnostics summary written: {0}" -f $summaryPath)
  Write-Log ("Diagnostics CSV written: {0}" -f $summaryCsvPath)
  Write-Log ("Diagnostics JSON written: {0}" -f $summaryJsonPath)
  return $diagRoot
}

Write-Log "INWC picker audit starting."
Write-Log ("TechRoot: {0}" -f $TechRoot)
Write-Log ("AuditRoot: {0}" -f $auditRoot)

if ([string]::IsNullOrWhiteSpace($AutoPilotFilePath)) {
  $AutoPilotFilePath = Join-Path $TechRoot "Configuration\Standards\Seed\NWC_Rehab_Seed.dgn"
}

Write-Log ("SkipLaunch={0}; KeepAppsOpen={1}; WaitSeconds={2}; UserActionSeconds={3}; WaitForEnterBeforeCapture={4}; AutoPilot={5}; AutoPilotUseSendKeys={6}" -f $SkipLaunch, $KeepAppsOpen, $WaitSeconds, $UserActionSeconds, $WaitForEnterBeforeCapture, $AutoPilot, $AutoPilotUseSendKeys)
if ($AutoPilot) {
  Write-Log ("AutoPilotFilePath={0}" -f $AutoPilotFilePath)
  if (-not $AutoPilotUseSendKeys) {
    Write-Log "AutoPilot mode is process-scoped (launch with file arg); SendKeys is disabled."
  } else {
    Write-Log "WARN: AutoPilotUseSendKeys is enabled; this is less reliable and can affect other windows."
  }
}

$apps = @(
  [pscustomobject]@{
    Name = "MicroStation 2025"
    Candidates = @(
      "C:\Program Files\Bentley\MicroStation 2025\MicroStation\microstation.exe",
      "C:\Program Files\Bentley\MicroStation CONNECT Edition\MicroStation\microstation.exe"
    )
    Patterns = @("microstation.exe")
  },
  [pscustomobject]@{
    Name = "OpenRoads Designer 2025.00"
    Candidates = @(
      "C:\Program Files\Bentley\OpenRoads Designer 2025.00\OpenRoadsDesigner\OpenRoadsDesigner.exe",
      "C:\Program Files\Bentley\OpenRoads Designer CONNECT Edition\OpenRoadsDesigner\OpenRoadsDesigner.exe"
    )
    Patterns = @("OpenRoadsDesigner.exe")
  }
)

$screens = New-Object System.Collections.Generic.List[string]

if (-not $SkipLaunch) {
  foreach ($app in $apps) {
    try {
      $exe = Resolve-Executable -ProductName $app.Name -Candidates $app.Candidates -NamePatterns $app.Patterns
      $screen = Invoke-AppPickerCapture -Name $app.Name -ExecutablePath $exe -TargetFilePath $AutoPilotFilePath
      if ($screen) { $screens.Add($screen) | Out-Null }
    } catch {
      Write-Log ("ERROR: {0}" -f $_.Exception.Message)
    }
  }
} else {
  Write-Log "SkipLaunch enabled: no apps were started."
}

$needDiagnostics = $PickerFailed
if (-not $NonInteractive -and -not $PickerFailed) {
  $answer = Read-Host "If the picker failed or looked wrong, type Y to export diagnostics now"
  if ($answer -match '^(?i)y(es)?$') {
    $needDiagnostics = $true
  }
}

if ($needDiagnostics) {
  Write-Log "Collecting diagnostics because picker failure was indicated."
  [void](Export-EnvCheckSnapshot)
  $diagPath = Export-ConfigResolutionDiagnostics
  Write-Log ("Diagnostics bundle: {0}" -f $diagPath)
} else {
  Write-Log "Diagnostics skipped. Re-run with -PickerFailed to force diagnostics export."
}

Write-Log ""
Write-Log "Picker audit complete."
Write-Log ("Log: {0}" -f $logPath)
if ($screens.Count -gt 0) {
  foreach ($screen in $screens) {
    Write-Log ("Screenshot: {0}" -f $screen)
  }
}

exit 0

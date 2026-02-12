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
$ErrorActionPreference = 'Stop'

if ($AutoPilotUseSendKeys) {
  Write-Warning 'AutoPilotUseSendKeys is deprecated in C# CLI mode and will be ignored.'
}

if ($WhatIfPreference -and -not $SkipLaunch) {
  $SkipLaunch = $true
}

. (Join-Path $PSScriptRoot 'INWC.CliShim.ps1')

$cliArgs = @('audit', 'picker')
if ($AutoPilot) { $cliArgs += '--autopilot' }
if ($AutoPilotFilePath) { $cliArgs += '--autopilot-file-path'; $cliArgs += $AutoPilotFilePath }
if ($SkipLaunch) { $cliArgs += '--skip-launch' }
if ($KeepAppsOpen) { $cliArgs += '--keep-apps-open' }
if ($PickerFailed) { $cliArgs += '--picker-failed' }
if ($WaitForEnterBeforeCapture) { $cliArgs += '--wait-for-enter-before-capture' }
if ($NonInteractive) { $cliArgs += '--non-interactive' }
$cliArgs += '--wait-seconds'; $cliArgs += $WaitSeconds.ToString()
$cliArgs += '--user-action-seconds'; $cliArgs += $UserActionSeconds.ToString()

$exitCode = Invoke-INWCCli -TechRoot $TechRoot -CliArgs $cliArgs -PassVerbose:($VerbosePreference -ne 'SilentlyContinue')
exit $exitCode

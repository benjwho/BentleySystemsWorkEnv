#Requires -Version 5.1
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
  [string]$TechRoot = "C:\Users\zohar\Documents\INWC_RH\tech",
  [switch]$Fix
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'INWC.CliShim.ps1')

$cliArgs = @('fix', 'picker-roots')
if ($Fix) { $cliArgs += '--apply' }

$exitCode = Invoke-INWCCli -TechRoot $TechRoot -CliArgs $cliArgs -PassWhatIf:$WhatIfPreference -PassVerbose:($VerbosePreference -ne 'SilentlyContinue')
exit $exitCode

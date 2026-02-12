#Requires -Version 5.1
[CmdletBinding(SupportsShouldProcess = $true)]
param(
  [string]$TechRoot = "C:\Users\zohar\Documents\INWC_RH\tech",
  [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'INWC.CliShim.ps1')

$cliArgs = @('detect', 'integrations')
if ($Apply) { $cliArgs += '--apply' }

$exitCode = Invoke-INWCCli -TechRoot $TechRoot -CliArgs $cliArgs -PassWhatIf:$WhatIfPreference -PassVerbose:($VerbosePreference -ne 'SilentlyContinue')
exit $exitCode

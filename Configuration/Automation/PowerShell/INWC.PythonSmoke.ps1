#Requires -Version 5.1
[CmdletBinding()]
param(
  [string]$TechRoot = "C:\Users\zohar\Documents\INWC_RH\tech",
  [switch]$SkipRuntimeTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'INWC.CliShim.ps1')

$cliArgs = @('smoke', 'python')
if ($SkipRuntimeTest) { $cliArgs += '--skip-runtime-test' }

$exitCode = Invoke-INWCCli -TechRoot $TechRoot -CliArgs $cliArgs -PassVerbose:($VerbosePreference -ne 'SilentlyContinue')
exit $exitCode

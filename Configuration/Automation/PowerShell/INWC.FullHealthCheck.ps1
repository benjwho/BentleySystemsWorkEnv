#Requires -Version 5.1
[CmdletBinding()]
param(
  [string]$TechRoot = "C:\Users\zohar\Documents\INWC_RH\tech",
  [switch]$SkipPythonRuntimeTest,
  [switch]$SkipProjectWiseWriteTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'INWC.CliShim.ps1')

$cliArgs = @('check', 'full-health')
if ($SkipPythonRuntimeTest) { $cliArgs += '--skip-python-runtime-test' }
if ($SkipProjectWiseWriteTest) { $cliArgs += '--skip-projectwise-write-test' }

$exitCode = Invoke-INWCCli -TechRoot $TechRoot -CliArgs $cliArgs -PassVerbose:($VerbosePreference -ne 'SilentlyContinue')
exit $exitCode

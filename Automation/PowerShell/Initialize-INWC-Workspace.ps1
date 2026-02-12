#Requires -Version 5.1
[CmdletBinding()]
param(
  [string]$TechRoot = "C:\Users\zohar\Documents\INWC_RH\tech"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'INWC.CliShim.ps1')

$cliArgs = @('init', 'workspace-report')
$exitCode = Invoke-INWCCli -TechRoot $TechRoot -CliArgs $cliArgs -PassVerbose:($VerbosePreference -ne 'SilentlyContinue')
exit $exitCode

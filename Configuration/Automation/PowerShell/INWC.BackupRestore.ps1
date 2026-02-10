#Requires -Version 5.1
[CmdletBinding(SupportsShouldProcess = $true, DefaultParameterSetName = 'Backup')]
param(
  [string]$TechRoot = "C:\Users\zohar\Documents\INWC_RH\tech",
  [string[]]$ProductConfigRoots = @(
    "C:\ProgramData\Bentley\MicroStation 2025\Configuration",
    "C:\ProgramData\Bentley\OpenRoads Designer 2025.00\Configuration",
    "C:\ProgramData\Bentley\OpenSite Designer 2025.00\Configuration",
    "C:\ProgramData\Bentley\OpenCities Map Ultimate 2025\Configuration",
    "C:\ProgramData\Bentley\Bentley Descartes 2025\Configuration",
    "C:\ProgramData\Bentley\OpenPlant 2024\Configuration"
  ),
  [switch]$IncludeUserPrefs,
  [Parameter(ParameterSetName = 'Restore', Mandatory = $true)]
  [string]$RestoreFrom
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'INWC.CliShim.ps1')

if ($PSCmdlet.ParameterSetName -eq 'Restore') {
  $cliArgs = @('backup', 'restore', '--from', $RestoreFrom)
} else {
  $cliArgs = @('backup', 'create')
  if ($IncludeUserPrefs) { $cliArgs += '--include-user-prefs' }
  foreach ($root in $ProductConfigRoots) {
    $cliArgs += '--product-config-root'
    $cliArgs += $root
  }
}

$exitCode = Invoke-INWCCli -TechRoot $TechRoot -CliArgs $cliArgs -PassWhatIf:$WhatIfPreference -PassVerbose:($VerbosePreference -ne 'SilentlyContinue')
exit $exitCode

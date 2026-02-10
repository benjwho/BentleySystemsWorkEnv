#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-INWCCli {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)] [string] $TechRoot,
    [Parameter(Mandatory)] [string[]] $CliArgs,
    [switch] $PassWhatIf,
    [switch] $PassVerbose,
    [switch] $PassJson
  )

  $resolvedRoot = (Resolve-Path -LiteralPath $TechRoot -ErrorAction Stop).Path
  $projectPath = Join-Path $resolvedRoot 'Configuration\Automation\DotNet\INWC.Automation.Cli\INWC.Automation.Cli.csproj'

  $candidateExes = @(
    (Join-Path $resolvedRoot 'Configuration\Automation\DotNet\INWC.Automation.Cli\bin\Release\net8.0-windows\win-x64\publish\inwc-cli.exe'),
    (Join-Path $resolvedRoot 'Configuration\Automation\DotNet\INWC.Automation.Cli\bin\Release\net8.0-windows\inwc-cli.exe'),
    (Join-Path $resolvedRoot 'Configuration\Automation\DotNet\INWC.Automation.Cli\bin\Debug\net8.0-windows\inwc-cli.exe')
  )

  $args = New-Object System.Collections.Generic.List[string]
  foreach ($arg in $CliArgs) { [void]$args.Add($arg) }
  [void]$args.Add('--tech-root')
  [void]$args.Add($resolvedRoot)

  if ($PassWhatIf) { [void]$args.Add('--what-if') }
  if ($PassVerbose) { [void]$args.Add('--verbose') }
  if ($PassJson) { [void]$args.Add('--json') }

  $exe = $candidateExes | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
  if ($exe) {
    & $exe @args | Out-Host
    return $LASTEXITCODE
  }

  if (!(Test-Path -LiteralPath $projectPath)) {
    throw "inwc-cli project not found: $projectPath"
  }

  $dotnetArgs = @('run', '--project', $projectPath, '--') + $args
  & dotnet @dotnetArgs | Out-Host
  return $LASTEXITCODE
}

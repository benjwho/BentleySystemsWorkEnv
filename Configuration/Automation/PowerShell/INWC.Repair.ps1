#Requires -Version 5.1
[CmdletBinding(SupportsShouldProcess=$true)]
param(
  [string]$TechRoot = "C:\Users\zohar\Documents\INWC_RH\tech",
  [string[]]$ProductConfigRoots = @(
    "C:\ProgramData\Bentley\MicroStation 2025\Configuration",
    "C:\ProgramData\Bentley\OpenRoads Designer 2025.00\Configuration",
    "C:\ProgramData\Bentley\OpenSite Designer 2025.00\Configuration",
    "C:\ProgramData\Bentley\OpenCities Map Ultimate 2025\Configuration",
    "C:\ProgramData\Bentley\Bentley Descartes 2025\Configuration",
    "C:\ProgramData\Bentley\OpenPlant 2024\Configuration"
  )
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Normalize-CfgPath([string]$p) {
  if (-not $p) { return $null }
  $x = $p.Trim() -replace "\\","/"
  if (-not $x.EndsWith("/")) { $x += "/" }
  return $x
}

function Get-CfgVarValue {
  param(
    [Parameter(Mandatory)] [string]$Path,
    [Parameter(Mandatory)] [string]$Var
  )
  if (!(Test-Path $Path)) { return $null }

  $rx = "^\s*{0}\s*=\s*(.*)\s*$" -f [regex]::Escape($Var)
  foreach ($line in (Get-Content -Path $Path)) {
    if ($line -match $rx) { return $Matches[1].Trim() }
  }
  return $null
}

function Set-UstnCustomConfiguration {
  param(
    [Parameter(Mandatory)] [string]$CfgPath,
    [Parameter(Mandatory)] [string]$ExpectedFwdWithSlash
  )

  if (!(Test-Path $CfgPath)) {
    Write-Warning "Missing: $CfgPath"
    return
  }

  $current = Get-CfgVarValue -Path $CfgPath -Var "_USTN_CUSTOM_CONFIGURATION"
  $currentNorm = Normalize-CfgPath $current
  $expectedNorm = Normalize-CfgPath $ExpectedFwdWithSlash

  if ($currentNorm -eq $expectedNorm) {
    Write-Host "OK (no change): $CfgPath"
    return
  }

  $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
  $bak = "$CfgPath.bak.$stamp"

  if ($PSCmdlet.ShouldProcess($CfgPath, "Backup + set _USTN_CUSTOM_CONFIGURATION=$expectedNorm")) {
    Copy-Item -Path $CfgPath -Destination $bak -Force

    $text = Get-Content -Path $CfgPath -Raw

    if ($text -notmatch '\[General\]') {
      $text = "[General]`r`n`r`n" + $text
    }

    $rxLine = '(^|\r?\n)\s*_USTN_CUSTOM_CONFIGURATION\s*=\s*.*?$'
    if ($text -match $rxLine) {
      $text = [regex]::Replace(
        $text,
        $rxLine,
        "`$1_USTN_CUSTOM_CONFIGURATION=$expectedNorm",
        [System.Text.RegularExpressions.RegexOptions]::Multiline
      )
    } else {
      $text = [regex]::Replace(
        $text,
        '(\[General\]\s*\r?\n)',
        "`$1_USTN_CUSTOM_CONFIGURATION=$expectedNorm`r`n",
        [System.Text.RegularExpressions.RegexOptions]::Singleline
      )
    }

    Set-Content -Path $CfgPath -Value $text -Encoding ASCII
    Write-Host "FIXED: $CfgPath  (backup: $bak)"
  }
}

$customCfgWin = Join-Path $TechRoot "Configuration"
$expectedCustomFwd = Normalize-CfgPath (($customCfgWin -replace "\\","/"))

foreach ($root in $ProductConfigRoots) {
  $cfgSetup = Join-Path $root "ConfigurationSetup.cfg"
  Set-UstnCustomConfiguration -CfgPath $cfgSetup -ExpectedFwdWithSlash $expectedCustomFwd
}

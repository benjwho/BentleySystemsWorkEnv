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
$ErrorActionPreference = "Stop"

function New-Stamp {
  return (Get-Date -Format "yyyyMMdd-HHmmss")
}

function Ensure-Directory {
  param([Parameter(Mandatory)] [string]$Path)

  if (Test-Path -LiteralPath $Path) { return }
  if ($PSCmdlet.ShouldProcess($Path, "Create directory")) {
    New-Item -Path $Path -ItemType Directory -Force | Out-Null
  }
}

function Safe-Name {
  param([string]$Value)
  return ($Value -replace '[:\\/ ]', '_')
}

function Backup-Item {
  param(
    [Parameter(Mandatory)] [string]$OriginalPath,
    [Parameter(Mandatory)] [string]$BackupPath,
    [Parameter(Mandatory)] [string]$Type,
    [Parameter(Mandatory)] [AllowEmptyCollection()] [System.Collections.Generic.List[object]]$Manifest
  )

  if (!(Test-Path -LiteralPath $OriginalPath)) {
    Write-Host "SKIP (missing): $OriginalPath"
    return
  }

  if ($PSCmdlet.ShouldProcess($OriginalPath, "Backup to $BackupPath")) {
    $parent = Split-Path -Path $BackupPath -Parent
    if (!(Test-Path -LiteralPath $parent)) {
      New-Item -Path $parent -ItemType Directory -Force | Out-Null
    }

    Copy-Item -LiteralPath $OriginalPath -Destination $BackupPath -Recurse -Force
    Write-Host "BACKUP: $OriginalPath -> $BackupPath"

    $Manifest.Add([pscustomobject]@{
      Type = $Type
      OriginalPath = $OriginalPath
      BackupPath = $BackupPath
    }) | Out-Null
  }
}

function Restore-Item {
  param(
    [Parameter(Mandatory)] [string]$OriginalPath,
    [Parameter(Mandatory)] [string]$BackupPath,
    [Parameter(Mandatory)] [string]$Type,
    [Parameter(Mandatory)] [string]$Stamp
  )

  if (!(Test-Path -LiteralPath $BackupPath)) {
    Write-Warning "Missing backup item (skipped): $BackupPath"
    return
  }

  if ($Type -eq "Directory") {
    if (Test-Path -LiteralPath $OriginalPath) {
      $renamed = "$OriginalPath.RESTORE_PREV.$Stamp"
      if ($PSCmdlet.ShouldProcess($OriginalPath, "Rename existing directory to $renamed")) {
        Rename-Item -LiteralPath $OriginalPath -NewName (Split-Path -Path $renamed -Leaf) -Force
        Write-Host "RENAMED: $OriginalPath -> $renamed"
      }
    }

    if ($PSCmdlet.ShouldProcess($OriginalPath, "Restore directory from $BackupPath")) {
      $parent = Split-Path -Path $OriginalPath -Parent
      if (!(Test-Path -LiteralPath $parent)) {
        New-Item -Path $parent -ItemType Directory -Force | Out-Null
      }
      Copy-Item -LiteralPath $BackupPath -Destination $OriginalPath -Recurse -Force
      Write-Host "RESTORED: $OriginalPath"
    }

    return
  }

  # File restore
  if (Test-Path -LiteralPath $OriginalPath) {
    $fileBackup = "$OriginalPath.preRestore.$Stamp"
    if ($PSCmdlet.ShouldProcess($OriginalPath, "Backup current file to $fileBackup")) {
      Copy-Item -LiteralPath $OriginalPath -Destination $fileBackup -Force
      Write-Host "PRE-RESTORE BACKUP: $fileBackup"
    }
  } else {
    $parent = Split-Path -Path $OriginalPath -Parent
    if (!(Test-Path -LiteralPath $parent) -and $PSCmdlet.ShouldProcess($parent, "Create parent directory")) {
      New-Item -Path $parent -ItemType Directory -Force | Out-Null
    }
  }

  if ($PSCmdlet.ShouldProcess($OriginalPath, "Restore file from $BackupPath")) {
    Copy-Item -LiteralPath $BackupPath -Destination $OriginalPath -Force
    Write-Host "RESTORED: $OriginalPath"
  }
}

$logsRoot = Join-Path $TechRoot "Logs"
$stamp = New-Stamp

if ($PSCmdlet.ParameterSetName -eq "Restore") {
  if (!(Test-Path -LiteralPath $RestoreFrom)) {
    throw "Restore path not found: $RestoreFrom"
  }

  $manifestPath = Join-Path $RestoreFrom "BackupManifest.csv"
  if (!(Test-Path -LiteralPath $manifestPath)) {
    throw "Backup manifest missing: $manifestPath"
  }

  $manifest = Import-Csv -LiteralPath $manifestPath
  foreach ($item in $manifest) {
    Restore-Item -OriginalPath $item.OriginalPath -BackupPath $item.BackupPath -Type $item.Type -Stamp $stamp
  }

  Write-Host ""
  Write-Host "Restore complete." -ForegroundColor Green
  Write-Host "Source backup: $RestoreFrom"
  exit 0
}

# Backup mode
$backupRoot = Join-Path $logsRoot ("BACKUP_{0}" -f $stamp)
Ensure-Directory -Path $backupRoot

$manifest = New-Object System.Collections.Generic.List[object]

$configRoot = Join-Path $TechRoot "Configuration"
$projectsRoot = Join-Path $TechRoot "Projects"

Backup-Item -OriginalPath $configRoot -BackupPath (Join-Path $backupRoot "TechRoot\Configuration") -Type "Directory" -Manifest $manifest
Backup-Item -OriginalPath $projectsRoot -BackupPath (Join-Path $backupRoot "TechRoot\Projects") -Type "Directory" -Manifest $manifest

foreach ($productRoot in $ProductConfigRoots) {
  $setupCfg = Join-Path $productRoot "ConfigurationSetup.cfg"
  $safe = Safe-Name -Value $productRoot
  Backup-Item -OriginalPath $setupCfg -BackupPath (Join-Path $backupRoot ("ProgramData\{0}\ConfigurationSetup.cfg" -f $safe)) -Type "File" -Manifest $manifest
}

if ($IncludeUserPrefs) {
  $prefsRoots = @(
    (Join-Path $env:LOCALAPPDATA "Bentley"),
    (Join-Path $env:TEMP "Bentley")
  )

  foreach ($prefsPath in $prefsRoots) {
    $safe = Safe-Name -Value $prefsPath
    Backup-Item -OriginalPath $prefsPath -BackupPath (Join-Path $backupRoot ("UserPrefs\{0}" -f $safe)) -Type "Directory" -Manifest $manifest
  }
}

$manifestPath = Join-Path $backupRoot "BackupManifest.csv"
if ($PSCmdlet.ShouldProcess($manifestPath, "Write backup manifest")) {
  $manifest | Export-Csv -Path $manifestPath -NoTypeInformation -Encoding ASCII
}

Write-Host ""
Write-Host "Backup complete." -ForegroundColor Green
Write-Host "Backup path: $backupRoot"
Write-Host "Manifest: $manifestPath"

exit 0

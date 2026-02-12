#Requires -Version 5.1
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
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
  [switch]$ResetUserPrefs,
  [switch]$RestoreOldestCfgBackup,
  [switch]$IncludeLogsInBackup
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function New-Stamp {
  return (Get-Date -Format "yyyyMMdd-HHmmss")
}

function To-FwdPath {
  param([string]$Path)
  return ($Path -replace "\\", "/")
}

function Ensure-Directory {
  param([Parameter(Mandatory)] [string]$Path)

  if (Test-Path -LiteralPath $Path) { return }
  if ($PSCmdlet.ShouldProcess($Path, "Create directory")) {
    New-Item -Path $Path -ItemType Directory -Force | Out-Null
  }
}

function Write-TextFile {
  param(
    [Parameter(Mandatory)] [string]$Path,
    [Parameter(Mandatory)] [string]$Content,
    [string]$BackupSuffix = $null
  )

  $current = if (Test-Path -LiteralPath $Path) { Get-Content -LiteralPath $Path -Raw -ErrorAction Stop } else { $null }
  if ($current -eq $Content) {
    Write-Host "OK (no change): $Path"
    return
  }

  if ($PSCmdlet.ShouldProcess($Path, "Write file")) {
    if ((Test-Path -LiteralPath $Path) -and $BackupSuffix) {
      $backupPath = "$Path.$BackupSuffix"
      Copy-Item -LiteralPath $Path -Destination $backupPath -Force
      Write-Host "Backup: $backupPath"
    }

    $parent = Split-Path -Path $Path -Parent
    if (!(Test-Path -LiteralPath $parent)) {
      New-Item -Path $parent -ItemType Directory -Force | Out-Null
    }

    Set-Content -LiteralPath $Path -Value $Content -Encoding ASCII
    Write-Host "WROTE: $Path"
  }
}

function Backup-ItemToRoot {
  param(
    [Parameter(Mandatory)] [string]$Source,
    [Parameter(Mandatory)] [string]$Destination
  )

  if (!(Test-Path -LiteralPath $Source)) {
    Write-Host "SKIP (missing source): $Source"
    return
  }

  $parent = Split-Path -Path $Destination -Parent
  if ($PSCmdlet.ShouldProcess($Source, "Backup to $Destination")) {
    if (!(Test-Path -LiteralPath $parent)) {
      New-Item -Path $parent -ItemType Directory -Force | Out-Null
    }

    Copy-Item -LiteralPath $Source -Destination $Destination -Recurse -Force
    Write-Host "BACKUP: $Source -> $Destination"
  }
}

function Comment-OutUstnCustomConfiguration {
  param(
    [Parameter(Mandatory)] [string]$ConfigPath,
    [Parameter(Mandatory)] [string]$Stamp
  )

  if (!(Test-Path -LiteralPath $ConfigPath)) {
    Write-Host "SKIP (missing): $ConfigPath"
    return
  }

  $original = Get-Content -LiteralPath $ConfigPath -Raw -ErrorAction Stop
  $updated = [regex]::Replace(
    $original,
    '(?m)^(\s*)_USTN_CUSTOM_CONFIGURATION\s*=',
    '$1#_USTN_CUSTOM_CONFIGURATION='
  )

  if ($updated -eq $original) {
    Write-Host "OK (no change): $ConfigPath"
    return
  }

  if ($PSCmdlet.ShouldProcess($ConfigPath, "Comment _USTN_CUSTOM_CONFIGURATION")) {
    $backupPath = "$ConfigPath.bak.$Stamp"
    Copy-Item -LiteralPath $ConfigPath -Destination $backupPath -Force
    Set-Content -LiteralPath $ConfigPath -Value $updated -Encoding ASCII
    Write-Host "EDITED: $ConfigPath"
    Write-Host "Backup: $backupPath"
  }
}

$stamp = New-Stamp
$cfgRoot = Join-Path $TechRoot "Configuration"
$projectsRoot = Join-Path $TechRoot "Projects"
$logsRoot = Join-Path $TechRoot "Logs"
$backupRoot = Join-Path $logsRoot ("RESET_BACKUP_{0}" -f $stamp)

Write-Host "TechRoot: $TechRoot"
Write-Host "BackupRoot: $backupRoot"

if ($PSCmdlet.ShouldProcess($backupRoot, "Create reset backup root")) {
  New-Item -Path $backupRoot -ItemType Directory -Force | Out-Null
}

# A) Backup current workspace content first
Backup-ItemToRoot -Source $cfgRoot -Destination (Join-Path $backupRoot "TechRoot\Configuration")
Backup-ItemToRoot -Source $projectsRoot -Destination (Join-Path $backupRoot "TechRoot\Projects")

if ($IncludeLogsInBackup) {
  Backup-ItemToRoot -Source $logsRoot -Destination (Join-Path $backupRoot "TechRoot\Logs")
}

# B) Backup and repair ProgramData pointers
foreach ($productRoot in $ProductConfigRoots) {
  $setupCfg = Join-Path $productRoot "ConfigurationSetup.cfg"
  if (!(Test-Path -LiteralPath $setupCfg)) {
    Write-Host "SKIP (missing): $setupCfg"
    continue
  }

  $safeName = ($productRoot -replace '[:\\ ]', '_')
  Backup-ItemToRoot -Source $setupCfg -Destination (Join-Path $backupRoot ("ProgramData\{0}\ConfigurationSetup.cfg" -f $safeName))

  if ($RestoreOldestCfgBackup) {
    $oldest = Get-ChildItem -Path $productRoot -Filter "ConfigurationSetup.cfg.bak.*" -ErrorAction SilentlyContinue |
      Sort-Object LastWriteTime |
      Select-Object -First 1

    if ($oldest) {
      if ($PSCmdlet.ShouldProcess($setupCfg, "Restore oldest backup: $($oldest.FullName)")) {
        Copy-Item -LiteralPath $setupCfg -Destination "$setupCfg.preReset.$stamp" -Force
        Copy-Item -LiteralPath $oldest.FullName -Destination $setupCfg -Force
        Write-Host "RESTORED: $setupCfg <= $($oldest.Name)"
      }
      continue
    }
  }

  Comment-OutUstnCustomConfiguration -ConfigPath $setupCfg -Stamp $stamp
}

# C) Rename current config folder (reversible)
if (Test-Path -LiteralPath $cfgRoot) {
  $purgedCfg = "$cfgRoot.PURGED.$stamp"
  if ($PSCmdlet.ShouldProcess($cfgRoot, "Rename to $purgedCfg")) {
    Rename-Item -LiteralPath $cfgRoot -NewName (Split-Path -Path $purgedCfg -Leaf) -Force
    Write-Host "RENAMED: $cfgRoot -> $purgedCfg"
  }
} else {
  Write-Host "SKIP (missing): $cfgRoot"
}

# D) Optional user prefs reset (reversible rename)
if ($ResetUserPrefs) {
  $userPrefsPaths = @(
    (Join-Path $env:LOCALAPPDATA "Bentley"),
    (Join-Path $env:TEMP "Bentley")
  )

  foreach ($prefsPath in $userPrefsPaths) {
    if (!(Test-Path -LiteralPath $prefsPath)) {
      Write-Host "SKIP (missing): $prefsPath"
      continue
    }

    $prefsSafe = ($prefsPath -replace '[:\\ ]', '_')
    Backup-ItemToRoot -Source $prefsPath -Destination (Join-Path $backupRoot ("UserPrefs\{0}" -f $prefsSafe))

    $renamed = "$prefsPath.PURGED.$stamp"
    if ($PSCmdlet.ShouldProcess($prefsPath, "Rename user prefs to $renamed")) {
      Rename-Item -LiteralPath $prefsPath -NewName (Split-Path -Path $renamed -Leaf) -Force
      Write-Host "RENAMED: $prefsPath -> $renamed"
    }
  }
}

# E) Rebuild minimal canonical structure
$requiredDirectories = @(
  (Join-Path $cfgRoot "Automation\PowerShell"),
  (Join-Path $cfgRoot "Automation\Python"),
  (Join-Path $cfgRoot "Organization"),
  (Join-Path $cfgRoot "Standards\Dgnlib"),
  (Join-Path $cfgRoot "Standards\Seed"),
  (Join-Path $cfgRoot "WorkSpaces"),
  (Join-Path $cfgRoot "WorkSets"),
  (Join-Path $projectsRoot "NWC_Rehab\Models"),
  (Join-Path $projectsRoot "NWC_Rehab\Sheets"),
  (Join-Path $projectsRoot "NWC_Rehab\Exports"),
  (Join-Path $TechRoot "Data"),
  (Join-Path $TechRoot "Deliverables"),
  (Join-Path $logsRoot "NWC_Rehab")
)

foreach ($dir in $requiredDirectories) {
  Ensure-Directory -Path $dir
}

$techRootFwd = To-FwdPath $TechRoot
$cfgRootFwd = To-FwdPath $cfgRoot
$workspacesRootFwd = (To-FwdPath (Join-Path $cfgRoot "WorkSpaces")).TrimEnd('/') + '/'
$worksetsRootFwd = (To-FwdPath (Join-Path $cfgRoot "WorkSets")).TrimEnd('/') + '/'

$workspaceSetupContent = @"
MS_CONFIGURATIONOPTS =

INWC_TECH = $techRootFwd
INWC_CFG = `$(INWC_TECH)/Configuration

MY_WORKSPACES_LOCATION = $workspacesRootFwd
MY_WORKSET_LOCATION = $worksetsRootFwd
_USTN_WORKSPACESROOT = `$(MY_WORKSPACES_LOCATION)
_USTN_WORKSETSROOT = `$(MY_WORKSET_LOCATION)

MY_STANDARDS_ROOT = `$(INWC_CFG)/Standards/
MY_CIVIL_ORGANIZATION_ROOT = `$(INWC_CFG)/Organization/

MY_PROJECTS_ROOT = `$(INWC_TECH)/Projects/
MY_DATA_ROOT = `$(INWC_TECH)/Data/
MY_DELIVERABLES_ROOT = `$(INWC_TECH)/Deliverables/
MY_LOGS_ROOT = `$(INWC_TECH)/Logs/
"@

$workspaceCfgContent = @"
INWC_TECH           = $techRootFwd
INWC_CFG            = `$(INWC_TECH)/Configuration
INWC_PROJECTS       = `$(INWC_TECH)/Projects
INWC_DATA           = `$(INWC_TECH)/Data
INWC_DELIVERABLES   = `$(INWC_TECH)/Deliverables
INWC_LOGS           = `$(INWC_TECH)/Logs
"@

$worksetCfgContent = @"
INWC_TECH           = $techRootFwd
INWC_PROJECT_ROOT   = `$(INWC_TECH)/Projects/NWC_Rehab
INWC_MODELS         = `$(INWC_PROJECT_ROOT)/Models
INWC_SHEETS         = `$(INWC_PROJECT_ROOT)/Sheets
INWC_EXPORT         = `$(INWC_PROJECT_ROOT)/Exports
INWC_WORKSET_LOGS   = `$(INWC_TECH)/Logs/NWC_Rehab

%include `$(INWC_CFG)/Organization/ByProject/NWC_Rehab/NWC_Rehab_Organization.cfg
"@

Write-TextFile -Path (Join-Path $cfgRoot "WorkSpaceSetup.cfg") -Content $workspaceSetupContent -BackupSuffix ("bak." + $stamp)
Write-TextFile -Path (Join-Path $cfgRoot "WorkSpaces\INWC_RH.cfg") -Content $workspaceCfgContent -BackupSuffix ("bak." + $stamp)
Write-TextFile -Path (Join-Path $cfgRoot "WorkSets\NWC_Rehab.cfg") -Content $worksetCfgContent -BackupSuffix ("bak." + $stamp)

# F) Set user-level _USTN_CUSTOM_CONFIGURATION
$envTarget = ($cfgRootFwd.TrimEnd('/') + '/')
$currentEnv = [Environment]::GetEnvironmentVariable("_USTN_CUSTOM_CONFIGURATION", "User")

if ($currentEnv -ne $envTarget) {
  if ($PSCmdlet.ShouldProcess("HKCU Environment", "Set _USTN_CUSTOM_CONFIGURATION=$envTarget")) {
    [Environment]::SetEnvironmentVariable("_USTN_CUSTOM_CONFIGURATION", $envTarget, "User")
    Write-Host "SET: User _USTN_CUSTOM_CONFIGURATION=$envTarget"
  }
} else {
  Write-Host "OK (no change): User _USTN_CUSTOM_CONFIGURATION already set"
}

Write-Host ""
Write-Host "Reset/Rebuild complete." -ForegroundColor Green
Write-Host "Backup path: $backupRoot"
Write-Host "Re-launch Bentley apps after completion."

exit 0

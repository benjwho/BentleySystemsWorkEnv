#Requires -Version 5.1
[CmdletBinding(SupportsShouldProcess=$true)]
param(
  [string]$TechRoot = "C:\Users\zohar\Documents\INWC_RH\tech",

  # Your installed product config roots
  [string[]]$ProductConfigRoots = @(
    "C:\ProgramData\Bentley\MicroStation 2025\Configuration",
    "C:\ProgramData\Bentley\OpenRoads Designer 2025.00\Configuration",
    "C:\ProgramData\Bentley\OpenSite Designer 2025.00\Configuration",
    "C:\ProgramData\Bentley\OpenCities Map Ultimate 2025\Configuration",
    "C:\ProgramData\Bentley\Bentley Descartes 2025\Configuration",
    "C:\ProgramData\Bentley\OpenPlant 2024\Configuration"
  ),

  # If set, also resets user prefs/cache (recommended if you want to purge "past customizations")
  [switch]$ResetUserPrefs,

  # If set, attempts to restore the OLDEST ConfigurationSetup.cfg backup if present (*.bak.*)
  [switch]$RestoreOldestCfgBackup,

  # If set, include TechRoot\Logs in backup copy (can be large)
  [switch]$IncludeLogsInBackup
)

function New-Stamp { Get-Date -Format "yyyyMMdd-HHmmss" }
function To-FwdPath([string]$p) { ($p -replace '\\','/') }

$stamp      = New-Stamp
$CfgRootWin = Join-Path $TechRoot "Configuration"
$CfgRootFwd = (To-FwdPath $CfgRootWin).TrimEnd('/') + "/"

$BackupRoot = Join-Path $TechRoot ("_Backups\RESET_BACKUP_{0}" -f $stamp)
New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null

Write-Host "BackupRoot: $BackupRoot"

# --- A) Backup what we are about to touch ---
$filesToBackup = New-Object System.Collections.Generic.List[string]
foreach ($root in $ProductConfigRoots) {
  $cfg = Join-Path $root "ConfigurationSetup.cfg"
  if (Test-Path $cfg) { $filesToBackup.Add($cfg) }
}
$customCfgToBackup = @(
  $CfgRootWin,
  (Join-Path $TechRoot "Projects")
)

foreach ($p in $filesToBackup) {
  $destDir = Join-Path $BackupRoot ("ProgramData\" + (Split-Path (Split-Path $p -Parent) -Leaf) )
  New-Item -ItemType Directory -Force -Path $destDir | Out-Null
  Copy-Item $p -Destination (Join-Path $destDir (Split-Path $p -Leaf)) -Force
}

foreach ($p in $customCfgToBackup) {
  if (Test-Path $p) {
    Copy-Item $p -Destination (Join-Path $BackupRoot "TechRoot") -Recurse -Force
  }
}

if ($IncludeLogsInBackup) {
  $logsRoot = Join-Path $TechRoot "Logs"
  if (Test-Path $logsRoot) {
    Copy-Item $logsRoot -Destination (Join-Path $BackupRoot "TechRoot") -Recurse -Force
  }
}

# --- B) PURGE your custom config folder(s) (rename, don't delete) ---
if (Test-Path $CfgRootWin) {
  $purged = "$CfgRootWin.PURGED.$stamp"
  if ($PSCmdlet.ShouldProcess($CfgRootWin, "Rename to $purged")) {
    Rename-Item -Path $CfgRootWin -NewName (Split-Path $purged -Leaf) -Force
  }
}

# --- C) Reset per-product ConfigurationSetup.cfg back to default-ish ---
# Cleaner approach: rely on Windows env var. So we comment out _USTN_CUSTOM_CONFIGURATION lines
# OR restore oldest *.bak.* if requested and available.
function CommentOut-UstnCustomConfig([string]$cfgPath) {
  $text = Get-Content $cfgPath -Raw
  # comment only if it's uncommented
  $new = [regex]::Replace(
    $text,
    '(^|\r?\n)\s*_USTN_CUSTOM_CONFIGURATION\s*=',
    "`$1#_USTN_CUSTOM_CONFIGURATION=",
    'Multiline'
  )
  if ($new -ne $text) {
    Set-Content -Path $cfgPath -Value $new -Encoding ASCII
    return $true
  }
  return $false
}

foreach ($root in $ProductConfigRoots) {
  $cfg = Join-Path $root "ConfigurationSetup.cfg"
  if (!(Test-Path $cfg)) { continue
  }

  $changed = $false

  if ($RestoreOldestCfgBackup) {
    $bak = Get-ChildItem -Path $root -Filter "ConfigurationSetup.cfg.bak.*" -ErrorAction SilentlyContinue |
           Sort-Object LastWriteTime |
           Select-Object -First 1
    if ($bak) {
      if ($PSCmdlet.ShouldProcess($cfg, "Restore oldest backup: $($bak.Name)")) {
        Copy-Item $cfg "$cfg.preReset.$stamp" -Force
        Copy-Item $bak.FullName $cfg -Force
        $changed = $true
        Write-Host "RESTORED: $cfg <= $($bak.Name)"
      }
    }
  }

  if (!$changed) {
    if ($PSCmdlet.ShouldProcess($cfg, "Comment out _USTN_CUSTOM_CONFIGURATION to allow Windows env var")) {
      $did = CommentOut-UstnCustomConfig $cfg
      if ($did) { Write-Host "EDITED: $cfg (commented _USTN_CUSTOM_CONFIGURATION)" }
      else      { Write-Host "OK (no change): $cfg" }
    }
  }
}

# --- D) Reset user prefs/cache (optional but matches your ask: "also in past") ---
if ($ResetUserPrefs) {
  # General Bentley guidance used by several DOT workflows: clear LocalAppData + Temp product folders to reset prefs/cache. :contentReference[oaicite:3]{index=3}
  $paths = @(
    (Join-Path $env:LOCALAPPDATA "Bentley"),
    (Join-Path $env:TEMP "Bentley")
  )

  foreach ($p in $paths) {
    if (Test-Path $p) {
      $newName = "$p.RENAMED.$stamp"
      if ($PSCmdlet.ShouldProcess($p, "Rename to $newName")) {
        Rename-Item -Path $p -NewName (Split-Path $newName -Leaf) -Force
        Write-Host "RENAMED: $p -> $newName"
      }
    }
  }
}

# --- E) Rebuild fresh structure (canonical WorkSets under Configuration\WorkSets) ---
$Dirs = @(
  "$CfgRootWin\Organization",
  "$CfgRootWin\WorkSpaces\INWC_RH",
  "$CfgRootWin\WorkSets",
  "$CfgRootWin\Standards\Dgnlib",
  "$CfgRootWin\Standards\Seed",
  "$CfgRootWin\Automation\PowerShell",
  "$CfgRootWin\Automation\Python",
  "$TechRoot\Projects\NWC_Rehab\Models",
  "$TechRoot\Projects\NWC_Rehab\Sheets",
  "$TechRoot\Projects\NWC_Rehab\Exports",
  "$TechRoot\Data",
  "$TechRoot\Deliverables",
  "$TechRoot\Logs"
)
$Dirs | ForEach-Object { New-Item -ItemType Directory -Force -Path $_ | Out-Null }

# WorkSpaceSetup.cfg: copy from MicroStation (best baseline), then patch locations + known WorkSet logic issue
$msWss = "C:\ProgramData\Bentley\MicroStation 2025\Configuration\WorkSpaceSetup.cfg"
$targetWss = Join-Path $CfgRootWin "WorkSpaceSetup.cfg"
if (!(Test-Path $targetWss)) {
  if (Test-Path $msWss) {
    Copy-Item $msWss $targetWss -Force
  } else {
    # minimal fallback
    @"
# WorkSpaceSetup.cfg (minimal fallback)
"@ | Set-Content -Path $targetWss -Encoding ASCII
  }
}

$orgRootFwd = (To-FwdPath (Join-Path $CfgRootWin "Organization")).TrimEnd('/') + "/"
$wsRootFwd  = (To-FwdPath (Join-Path $CfgRootWin "WorkSpaces")).TrimEnd('/') + "/"

# Ensure our variables exist (pattern used in DOT guides: MY_CIVIL_ORGANIZATION_ROOT + MY_WORKSPACES_LOCATION) :contentReference[oaicite:4]{index=4}
$wssText = Get-Content $targetWss -Raw

if ($wssText -notmatch '(?m)^\s*MY_CIVIL_ORGANIZATION_ROOT\s*=') {
  $wssText += "`r`nMY_CIVIL_ORGANIZATION_ROOT = $orgRootFwd`r`n"
} else {
  $wssText = [regex]::Replace($wssText, '(?m)^\s*MY_CIVIL_ORGANIZATION_ROOT\s*=.*$', "MY_CIVIL_ORGANIZATION_ROOT = $orgRootFwd")
}

if ($wssText -notmatch '(?m)^\s*MY_WORKSPACES_LOCATION\s*=') {
  $wssText += "MY_WORKSPACES_LOCATION = $wsRootFwd`r`n"
} else {
  $wssText = [regex]::Replace($wssText, '(?m)^\s*MY_WORKSPACES_LOCATION\s*=.*$', "MY_WORKSPACES_LOCATION = $wsRootFwd")
}

# Patch the known WorkSet-location logic issue (some delivered WorkSpaceSetup.cfg used MY_WORKSPACES_LOCATION where MY_WORKSET_LOCATION should be used) :contentReference[oaicite:5]{index=5}
# This patch is SAFE even if you don't use MY_WORKSET_LOCATION: it preserves default else branch.
$wssText = $wssText -replace 'MY_WORKSPACES_LOCATION\)\)\$?\(_USTN_WORKSPACENAME\)', 'MY_WORKSET_LOCATION))$(_USTN_WORKSPACENAME)'

Set-Content -Path $targetWss -Value $wssText -Encoding ASCII

# Workspace cfg (enumerated from WorkSpaces root)
$wsCfg = Join-Path $CfgRootWin "WorkSpaces\INWC_RH.cfg"
@"
INWC_TECH           = $(To-FwdPath $TechRoot)
INWC_CFG            = $(To-FwdPath $CfgRootWin)
INWC_PROJECTS       = `$(INWC_TECH)/Projects
INWC_DATA           = `$(INWC_TECH)/Data
INWC_DELIVERABLES   = `$(INWC_TECH)/Deliverables
INWC_LOGS           = `$(INWC_TECH)/Logs
"@ | Set-Content -Path $wsCfg -Encoding ASCII

# WorkSet cfg (canonical location under Configuration\WorkSets)
$wsetCfg = Join-Path $CfgRootWin "WorkSets\NWC_Rehab.cfg"
@"
INWC_TECH           = $(To-FwdPath $TechRoot)
INWC_PROJECT_ROOT   = `$(INWC_TECH)/Projects/NWC_Rehab
INWC_MODELS         = `$(INWC_PROJECT_ROOT)/Models
INWC_SHEETS         = `$(INWC_PROJECT_ROOT)/Sheets
INWC_EXPORT         = `$(INWC_PROJECT_ROOT)/Exports
INWC_WORKSET_LOGS   = `$(INWC_TECH)/Logs/NWC_Rehab

# --- Include project-specific organization standards ---
%include `$(INWC_CFG)/Organization/ByProject/NWC_Rehab/NWC_Rehab_Organization.cfg
"@ | Set-Content -Path $wsetCfg -Encoding ASCII

# --- F) Set Windows user env var for _USTN_CUSTOM_CONFIGURATION (ONE place for ALL products) ---
# Trailing slash is required per Bentley workflows. :contentReference[oaicite:6]{index=6}
$envValue = $CfgRootFwd  # forward slashes + trailing /
if ($PSCmdlet.ShouldProcess("HKCU Environment", "Set _USTN_CUSTOM_CONFIGURATION=$envValue")) {
  [Environment]::SetEnvironmentVariable("_USTN_CUSTOM_CONFIGURATION", $envValue, "User")
  Write-Host "_USTN_CUSTOM_CONFIGURATION (User) set to: $envValue"
}

Write-Host "`nDONE. Reboot or sign-out/sign-in is sometimes needed for apps to see new env vars." -ForegroundColor Yellow
Write-Host "Then launch MicroStation/ORD and confirm WorkSpace=INWC_RH, WorkSet=NWC_Rehab."

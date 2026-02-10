#Requires -Version 5.1
<##
  INWC.WorkspacePickerFix.ps1

  Default mode is read-only validation.
  Use -Fix to apply minimal, idempotent edits to WorkSpaceSetup.cfg.
##>

[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
  [string]$TechRoot = "C:\Users\zohar\Documents\INWC_RH\tech",
  [switch]$Fix
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Normalize-CfgRoot {
  param([string]$Value)
  if ([string]::IsNullOrWhiteSpace($Value)) { return $null }

  $normalized = $Value.Trim().Trim('"', "'") -replace "\\", "/"
  $normalized = $normalized.TrimEnd("/")
  if ([string]::IsNullOrWhiteSpace($normalized)) { return $null }
  return ($normalized + "/")
}

function Get-CfgVarValue {
  param(
    [Parameter(Mandatory)] [string]$Path,
    [Parameter(Mandatory)] [string]$Var
  )

  if (!(Test-Path -LiteralPath $Path)) { return $null }

  $rx = "^\s*{0}\s*=\s*(.*?)\s*$" -f [regex]::Escape($Var)
  $last = $null
  foreach ($line in (Get-Content -LiteralPath $Path -ErrorAction Stop)) {
    if ($line -match '^\s*[#;]') { continue }
    if ($line -match $rx) { $last = $Matches[1].Trim() }
  }
  return $last
}

function Add-Result {
  param(
    [System.Collections.Generic.List[object]]$Bucket,
    [string]$Scope,
    [string]$Check,
    [string]$Target,
    [bool]$OK,
    [string]$Detail = ""
  )

  $Bucket.Add([pscustomobject]@{
    Scope  = $Scope
    Check  = $Check
    Target = $Target
    OK     = $OK
    Detail = $Detail
  }) | Out-Null
}

function Set-ManagedBlock {
  param(
    [Parameter(Mandatory)] [string]$Text,
    [Parameter(Mandatory)] [string]$ManagedBlock
  )

  $begin = "# --- BEGIN INWC_PICKER_ROOTS (managed) ---"
  $end = "# --- END INWC_PICKER_ROOTS (managed) ---"
  $blockPattern = "(?ms)^\s*#\s*--- BEGIN INWC_PICKER_ROOTS \(managed\) ---\s*\r?\n.*?^\s*#\s*--- END INWC_PICKER_ROOTS \(managed\) ---\s*\r?\n?"

  $base = $Text
  if ($base -match $blockPattern) {
    $base = [regex]::Replace($base, $blockPattern, "", 1)
  }

  $base = $base.TrimEnd("`r", "`n")
  if ([string]::IsNullOrWhiteSpace($base)) {
    return ($ManagedBlock + "`r`n")
  }

  return ($base + "`r`n`r`n" + $ManagedBlock + "`r`n")
}

$cfgRootWin = Join-Path $TechRoot "Configuration"
$workSpaceSetupPath = Join-Path $cfgRootWin "WorkSpaceSetup.cfg"

$expectedWorkSpacesRoot = Normalize-CfgRoot (Join-Path $cfgRootWin "WorkSpaces")
$expectedWorkSetsRoot = Normalize-CfgRoot (Join-Path $cfgRootWin "WorkSets")

$results = New-Object System.Collections.Generic.List[object]

Add-Result -Bucket $results -Scope "Local" -Check "WorkSpaceSetup exists" -Target $workSpaceSetupPath -OK (Test-Path -LiteralPath $workSpaceSetupPath)
Add-Result -Bucket $results -Scope "Local" -Check "WorkSpaces folder exists" -Target (Join-Path $cfgRootWin "WorkSpaces") -OK (Test-Path -LiteralPath (Join-Path $cfgRootWin "WorkSpaces"))
Add-Result -Bucket $results -Scope "Local" -Check "WorkSets folder exists" -Target (Join-Path $cfgRootWin "WorkSets") -OK (Test-Path -LiteralPath (Join-Path $cfgRootWin "WorkSets"))

if (Test-Path -LiteralPath $workSpaceSetupPath) {
  $currentMyWorkSpaces = Get-CfgVarValue -Path $workSpaceSetupPath -Var "MY_WORKSPACES_LOCATION"
  $currentMyWorkSets = Get-CfgVarValue -Path $workSpaceSetupPath -Var "MY_WORKSET_LOCATION"
  $currentUstnWorkSpaces = Get-CfgVarValue -Path $workSpaceSetupPath -Var "_USTN_WORKSPACESROOT"
  $currentUstnWorkSets = Get-CfgVarValue -Path $workSpaceSetupPath -Var "_USTN_WORKSETSROOT"

  Add-Result -Bucket $results -Scope "WorkSpaceSetup" -Check "MY_WORKSPACES_LOCATION" -Target $expectedWorkSpacesRoot -OK ((Normalize-CfgRoot $currentMyWorkSpaces) -eq $expectedWorkSpacesRoot) -Detail $currentMyWorkSpaces
  Add-Result -Bucket $results -Scope "WorkSpaceSetup" -Check "MY_WORKSET_LOCATION" -Target $expectedWorkSetsRoot -OK ((Normalize-CfgRoot $currentMyWorkSets) -eq $expectedWorkSetsRoot) -Detail $currentMyWorkSets

  $wsRootOk = ($currentUstnWorkSpaces -eq '$(MY_WORKSPACES_LOCATION)') -or ((Normalize-CfgRoot $currentUstnWorkSpaces) -eq $expectedWorkSpacesRoot)
  $wsetRootOk = ($currentUstnWorkSets -eq '$(MY_WORKSET_LOCATION)') -or ((Normalize-CfgRoot $currentUstnWorkSets) -eq $expectedWorkSetsRoot)

  Add-Result -Bucket $results -Scope "WorkSpaceSetup" -Check "_USTN_WORKSPACESROOT" -Target $expectedWorkSpacesRoot -OK $wsRootOk -Detail $currentUstnWorkSpaces
  Add-Result -Bucket $results -Scope "WorkSpaceSetup" -Check "_USTN_WORKSETSROOT" -Target $expectedWorkSetsRoot -OK $wsetRootOk -Detail $currentUstnWorkSets
}

$results | Format-Table -AutoSize

$needsFix = ($results.OK -contains $false)

if (-not $Fix) {
  if ($needsFix) {
    Write-Host "`nCHECK: picker variables need normalization. Re-run with -Fix to apply changes." -ForegroundColor Yellow
    exit 2
  }

  Write-Host "`nCHECK: picker variables are already valid." -ForegroundColor Green
  exit 0
}

if (!(Test-Path -LiteralPath $workSpaceSetupPath)) {
  Write-Host "`nFIX: WorkSpaceSetup.cfg is missing. Cannot apply fix." -ForegroundColor Red
  exit 1
}

if (-not $needsFix) {
  Write-Host "No change: WorkSpaceSetup.cfg already has correct picker roots." -ForegroundColor Green
  exit 0
}

$managedBlock = @"
# --- BEGIN INWC_PICKER_ROOTS (managed) ---
MY_WORKSPACES_LOCATION = $expectedWorkSpacesRoot
MY_WORKSET_LOCATION = $expectedWorkSetsRoot
_USTN_WORKSPACESROOT = `$(MY_WORKSPACES_LOCATION)
_USTN_WORKSETSROOT = `$(MY_WORKSET_LOCATION)
# --- END INWC_PICKER_ROOTS (managed) ---
"@

$originalText = Get-Content -LiteralPath $workSpaceSetupPath -Raw -ErrorAction Stop
$updatedText = Set-ManagedBlock -Text $originalText -ManagedBlock $managedBlock

if ($updatedText -eq $originalText) {
  Write-Host "No change: WorkSpaceSetup.cfg already matches managed picker block." -ForegroundColor Green
  exit 0
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupPath = "$workSpaceSetupPath.bak.$timestamp"

if ($PSCmdlet.ShouldProcess($workSpaceSetupPath, "Backup and normalize picker roots")) {
  Copy-Item -LiteralPath $workSpaceSetupPath -Destination $backupPath -Force
  Set-Content -LiteralPath $workSpaceSetupPath -Value $updatedText -Encoding ASCII
  Write-Host "FIXED: $workSpaceSetupPath" -ForegroundColor Green
  Write-Host "Backup: $backupPath"
}

exit 0

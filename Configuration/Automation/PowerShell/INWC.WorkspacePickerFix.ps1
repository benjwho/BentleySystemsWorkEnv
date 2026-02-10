#Requires -Version 5.1
<#
  INWC.WorkspacePickerFix.ps1
  Purpose:
    Ensure Bentley Workspace/WorkSet picker enumerates from:
      C:\Users\zohar\Documents\INWC_RH\tech\Configuration\WorkSpaces
      C:\Users\zohar\Documents\INWC_RH\tech\Configuration\WorkSets

  Default: CHECK only (no changes)
  Use -Fix to apply minimal edits to WorkSpaceSetup.cfg (with backup)
#>

[CmdletBinding(SupportsShouldProcess=$true)]
param(
  [string]$TechRoot = "C:\Users\zohar\Documents\INWC_RH\tech",
  [switch]$Fix
)

function Get-CfgVarValue {
  param(
    [Parameter(Mandatory)] [string]$Path,
    [Parameter(Mandatory)] [string]$Var
  )
  if (!(Test-Path $Path)) { return $null }

  $rx = "^\s*{0}\s*=\s*(.*?)\s*$" -f [regex]::Escape($Var)
  foreach ($line in (Get-Content $Path -ErrorAction Stop)) {
    if ($line -match $rx) { return $Matches[1].Trim() }
  }
  return $null
}

function Set-OrAppendCfgVar {
  param(
    [Parameter(Mandatory)] [string]$Text,
    [Parameter(Mandatory)] [string]$Var,
    [Parameter(Mandatory)] [string]$Value
  )
  $rx = "(?m)^\s*{0}\s*=\s*.*$" -f [regex]::Escape($Var)
  if ($Text -match $rx) {
    return [regex]::Replace($Text, $rx, ("{0}={1}" -f $Var, $Value))
  } else {
    return ($Text.TrimEnd() + "`r`n{0}={1}`r`n" -f $Var, $Value)
  }
}

$customCfgWin = Join-Path $TechRoot "Configuration"
$wsSetupPath  = Join-Path $customCfgWin "WorkSpaceSetup.cfg"

# Forward-slash + trailing slash (Bentley concatenates paths)  (see training docs)
$customCfgFwd = (($customCfgWin -replace '\\','/') + "/")
$expectedWorkSpacesRoot = ($customCfgFwd + "WorkSpaces/")
$expectedWorkSetsRoot   = ($customCfgFwd + "WorkSets/")

$results = New-Object System.Collections.Generic.List[object]

# A) Existence
$results.Add([pscustomobject]@{ Scope="Local"; Check="WorkSpaceSetup.cfg exists"; Target=$wsSetupPath; OK=(Test-Path $wsSetupPath); Detail="" })
$results.Add([pscustomobject]@{ Scope="Local"; Check="WorkSpaces folder exists"; Target=(Join-Path $customCfgWin "WorkSpaces"); OK=(Test-Path (Join-Path $customCfgWin "WorkSpaces")); Detail="" })
$results.Add([pscustomobject]@{ Scope="Local"; Check="WorkSets folder exists"; Target=(Join-Path $customCfgWin "WorkSets"); OK=(Test-Path (Join-Path $customCfgWin "WorkSets")); Detail="" })

# B) Current values (if file exists)
if (Test-Path $wsSetupPath) {
  $curMyWs   = Get-CfgVarValue -Path $wsSetupPath -Var "MY_WORKSPACES_LOCATION"
  $curMyWset = Get-CfgVarValue -Path $wsSetupPath -Var "MY_WORKSET_LOCATION"
  $curWsRoot = Get-CfgVarValue -Path $wsSetupPath -Var "_USTN_WORKSPACESROOT"
  $curWsetRoot = Get-CfgVarValue -Path $wsSetupPath -Var "_USTN_WORKSETSROOT"

  $results.Add([pscustomobject]@{ Scope="WorkSpaceSetup"; Check="MY_WORKSPACES_LOCATION"; Target=$expectedWorkSpacesRoot; OK=($curMyWs -eq $expectedWorkSpacesRoot); Detail=$curMyWs })
  $results.Add([pscustomobject]@{ Scope="WorkSpaceSetup"; Check="MY_WORKSET_LOCATION";   Target=$expectedWorkSetsRoot;   OK=($curMyWset -eq $expectedWorkSetsRoot);   Detail=$curMyWset })

  # Accept either direct root or via indirection
  $wsOk   = ($curWsRoot -eq $expectedWorkSpacesRoot) -or ($curWsRoot -eq '$(MY_WORKSPACES_LOCATION)')
  $wsetOk = ($curWsetRoot -eq $expectedWorkSetsRoot) -or ($curWsetRoot -eq '$(MY_WORKSET_LOCATION)')

  $results.Add([pscustomobject]@{ Scope="WorkSpaceSetup"; Check="_USTN_WORKSPACESROOT"; Target=$expectedWorkSpacesRoot; OK=$wsOk; Detail=$curWsRoot })
  $results.Add([pscustomobject]@{ Scope="WorkSpaceSetup"; Check="_USTN_WORKSETSROOT";   Target=$expectedWorkSetsRoot;   OK=$wsetOk; Detail=$curWsetRoot })
}

$results | Format-Table -AutoSize

$needsFix = ($results.OK -contains $false)

if (!$Fix) {
  if ($needsFix) {
    Write-Host "`nCHECK: Not ready for picker enumeration. Re-run with -Fix to apply minimal edits." -ForegroundColor Yellow
    exit 2
  } else {
    Write-Host "`nCHECK: Looks good for picker enumeration." -ForegroundColor Green
    exit 0
  }
}

# --- Apply minimal safe fix ---
if (!(Test-Path $wsSetupPath)) {
  Write-Host "`nFIX: WorkSpaceSetup.cfg missing; cannot fix automatically." -ForegroundColor Red
  exit 1
}

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$bak = "$wsSetupPath.bak.$stamp"

Copy-Item $wsSetupPath $bak -Force
Write-Host "Backup: $bak"

$text = Get-Content $wsSetupPath -Raw

# Append/override the critical roots at the END (last assignment wins)
$text = $text.TrimEnd() + "`r`n`r`n# --- INWC override (picker roots) ---`r`n"
$text = Set-OrAppendCfgVar -Text $text -Var "MY_WORKSPACES_LOCATION" -Value $expectedWorkSpacesRoot
$text = Set-OrAppendCfgVar -Text $text -Var "MY_WORKSET_LOCATION"     -Value $expectedWorkSetsRoot
$text = Set-OrAppendCfgVar -Text $text -Var "_USTN_WORKSPACESROOT"    -Value '$(MY_WORKSPACES_LOCATION)'
$text = Set-OrAppendCfgVar -Text $text -Var "_USTN_WORKSETSROOT"      -Value '$(MY_WORKSET_LOCATION)'

if ($PSCmdlet.ShouldProcess($wsSetupPath, "Write updated WorkSpaceSetup.cfg")) {
  Set-Content -Path $wsSetupPath -Value $text -Encoding ASCII
  Write-Host "`nFIX: Updated WorkSpaceSetup.cfg. Close ALL Bentley apps and re-launch." -ForegroundColor Green
}

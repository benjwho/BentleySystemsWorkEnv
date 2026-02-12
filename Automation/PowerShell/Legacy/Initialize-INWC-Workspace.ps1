#Requires -Version 5.1
<#
.SYNOPSIS
    Initialize and validate the INWC workspace environment.

.DESCRIPTION
    Performs comprehensive checks on workspace structure, configuration files,
    and environment variables. Sets up the environment for MicroStation use.

.PARAMETER TechRoot
    Root path to the INWC tech directory (default: C:\Users\zohar\Documents\INWC_RH\tech)

.EXAMPLE
    .\Initialize-INWC-Workspace.ps1
    .\Initialize-INWC-Workspace.ps1 -TechRoot "C:\Custom\Path\tech"
#>

[CmdletBinding()]
param(
    [string]$TechRoot = "C:\Users\zohar\Documents\INWC_RH\tech"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ============================================================================
# Console Functions
# ============================================================================

function Write-Title([string]$text) {
    Write-Host "`n
╔════════════════════════════════════════════════════════════════╗`n║ $($text.PadRight(62)) ║`n╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
}

function Write-CheckMark([string]$text) {
    Write-Host "  ✓ $text" -ForegroundColor Green
}

function Write-CrossMark([string]$text) {
    Write-Host "  ✗ $text" -ForegroundColor Red
}

function Write-Warning-Msg([string]$text) {
    Write-Host "  ⚠ $text" -ForegroundColor Yellow
}

# ============================================================================
# Validation Functions
# ============================================================================

function Test-PathExists([string]$path, [string]$description) {
    if (Test-Path $path) {
        Write-CheckMark "$description"
        return $true
    } else {
        Write-CrossMark "$description (NOT FOUND)"
        return $false
    }
}

function Validate-ConfigFile([string]$path, [string]$variable) {
    if (-not (Test-Path $path)) {
        Write-CrossMark "Config file missing: $path"
        return $false
    }

    $content = Get-Content -Path $path -Raw
    if ($content -match "$([regex]::Escape($variable))\s*=") {
        Write-CheckMark "Variable '$variable' found in $([System.IO.Path]::GetFileName($path))"
        return $true
    } else {
        Write-Warning-Msg "Variable '$variable' NOT found in $([System.IO.Path]::GetFileName($path))"
        return $false
    }
}

# ============================================================================
# Main Validation
# ============================================================================

Write-Title "INWC Workspace Initialization"

Write-Host "Tech Root: $TechRoot`n" -ForegroundColor Gray

$allValid = $true

# --- Phase 1: Directory Structure ---
Write-Title "Phase 1: Directory Structure"

$directories = @(
    @{ Path = "$TechRoot\Configuration"; Desc = "Configuration root" }
    @{ Path = "$TechRoot\Configuration\Standards"; Desc = "Standards folder" }
    @{ Path = "$TechRoot\Configuration\Standards\Seed"; Desc = "Seed DGN directory" }
    @{ Path = "$TechRoot\Configuration\Standards\Cells"; Desc = "Cell library directory" }
    @{ Path = "$TechRoot\Configuration\Standards\Dgnlib"; Desc = "DGN library directory" }
    @{ Path = "$TechRoot\Configuration\Standards\Fonts"; Desc = "Fonts directory" }
    @{ Path = "$TechRoot\Configuration\Standards\LineStyles"; Desc = "LineStyles directory" }
    @{ Path = "$TechRoot\Configuration\Standards\Reports"; Desc = "Reports directory" }
    @{ Path = "$TechRoot\Configuration\Organization"; Desc = "Organization root" }
    @{ Path = "$TechRoot\Configuration\Organization\ByDiscipline\Civil"; Desc = "Civil discipline folder" }
    @{ Path = "$TechRoot\Configuration\Organization\ByProject\NWC_Rehab"; Desc = "NWC_Rehab project folder" }
    @{ Path = "$TechRoot\Configuration\Organization\ByFunction\Plotting"; Desc = "Plotting standards folder" }
    @{ Path = "$TechRoot\Configuration\Organization\ByFunction\Exports"; Desc = "Export standards folder" }
    @{ Path = "$TechRoot\Configuration\WorkSpaces"; Desc = "Workspaces folder" }
    @{ Path = "$TechRoot\Configuration\WorkSets"; Desc = "WorkSets folder" }
    @{ Path = "$TechRoot\Automation\PowerShell"; Desc = "PowerShell automation" }
    @{ Path = "$TechRoot\Projects\NWC_Rehab\Models"; Desc = "NWC_Rehab Models folder" }
    @{ Path = "$TechRoot\Projects\NWC_Rehab\Sheets"; Desc = "NWC_Rehab Sheets folder" }
    @{ Path = "$TechRoot\Projects\NWC_Rehab\Exports"; Desc = "NWC_Rehab Exports folder" }
    @{ Path = "$TechRoot\Logs"; Desc = "Logs folder" }
)

foreach ($dir in $directories) {
    $valid = Test-PathExists $dir.Path $dir.Desc
    $allValid = $allValid -and $valid
}

# --- Phase 2: Configuration Files ---
Write-Title "Phase 2: Configuration Files"

$configFiles = @(
    @{ Path = "$TechRoot\Configuration\WorkSpaceSetup.cfg"; Variable = "MY_WORKSPACES_LOCATION" }
    @{ Path = "$TechRoot\Configuration\WorkSpaces\INWC_RH.cfg"; Variable = "INWC_TECH" }
    @{ Path = "$TechRoot\Configuration\WorkSets\NWC_Rehab.cfg"; Variable = "INWC_PROJECT_ROOT" }
    @{ Path = "$TechRoot\Configuration\Organization\ByProject\NWC_Rehab\NWC_Rehab_Organization.cfg"; Variable = "INWC_PROJECT_NAME" }
    @{ Path = "$TechRoot\Configuration\Organization\ByDiscipline\Civil\Civil_Standards.cfg"; Variable = "CIVIL_SEED_FILE" }
    @{ Path = "$TechRoot\Configuration\Organization\ByFunction\Exports\Export_Standards.cfg"; Variable = "EXPORT_PDF_ENABLED" }
)

foreach ($config in $configFiles) {
    $valid = Validate-ConfigFile $config.Path $config.Variable
    $allValid = $allValid -and $valid
}

# --- Phase 3: Environment Variables ---
Write-Title "Phase 3: Setting Environment Variables"

$env:INWC_TECH = $TechRoot
$env:INWC_CFG = "$TechRoot\Configuration"
$env:INWC_PROJECTS = "$TechRoot\Projects"
$env:INWC_DATA = "$TechRoot\Data"
$env:INWC_DELIVERABLES = "$TechRoot\Deliverables"
$env:INWC_LOGS = "$TechRoot\Logs"

Write-CheckMark "INWC_TECH = $env:INWC_TECH"
Write-CheckMark "INWC_CFG = $env:INWC_CFG"
Write-CheckMark "INWC_PROJECTS = $env:INWC_PROJECTS"
Write-CheckMark "INWC_DATA = $env:INWC_DATA"
Write-CheckMark "INWC_DELIVERABLES = $env:INWC_DELIVERABLES"
Write-CheckMark "INWC_LOGS = $env:INWC_LOGS"

# --- Phase 4: Workspace Configuration Reference ---
Write-Title "Phase 4: Workspace Configuration Status"

Write-Host "`nWorkspace Root: INWC_RH" -ForegroundColor Cyan
Write-Host "  • Location: $TechRoot\Configuration\WorkSpaces\INWC_RH.cfg"
Write-Host "  • Current Project: NWC_Rehab`n"

Write-Host "Project Details:" -ForegroundColor Cyan
Write-Host "  • Name: NWC_Rehab"
Write-Host "  • Models:  $TechRoot\Projects\NWC_Rehab\Models"
Write-Host "  • Sheets:  $TechRoot\Projects\NWC_Rehab\Sheets"
Write-Host "  • Exports: $TechRoot\Projects\NWC_Rehab\Exports`n"

# --- Phase 5: Bentley Product Configuration Check ---
Write-Title "Phase 5: Bentley Products Integration Check"

$bentleyProducts = @(
    "C:\ProgramData\Bentley\MicroStation 2025\Configuration"
    "C:\ProgramData\Bentley\OpenRoads Designer 2025.00\Configuration"
    "C:\ProgramData\Bentley\OpenSite Designer 2025.00\Configuration"
    "C:\ProgramData\Bentley\OpenCities Map Ultimate 2025\Configuration"
)

Write-Warning "NOTE: Manual configuration required in ProgramData`n"

foreach ($prodConfig in $bentleyProducts) {
    $cfgSetup = Join-Path $prodConfig "ConfigurationSetup.cfg"
    if (Test-Path $cfgSetup) {
        $prodName = Split-Path (Split-Path $prodConfig -Parent) -Leaf
        Write-CheckMark "$prodName - ConfigurationSetup.cfg found"
        Write-Host "     ➔ Add this line to the file:`n" -ForegroundColor Gray
        Write-Host "       _USTN_CUSTOM_CONFIGURATION = C:/Users/zohar/Documents/INWC_RH/tech/Configuration/" -ForegroundColor Yellow
    }
}

# --- Phase 6: Final Summary ---
Write-Title "Validation Summary"

if ($allValid) {
    Write-Host "`n✓ All workspace components validated successfully!`n" -ForegroundColor Green
    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host "  1. Edit ProgramData ConfigurationSetup.cfg files (see Phase 5)"
    Write-Host "  2. Launch MicroStation 2025"
    Write-Host "  3. File → Open Workspace → Select 'INWC_RH'"
    Write-Host "  4. Select 'NWC_Rehab' workset"
    Write-Host "  5. Verify all paths resolve correctly`n"
    exit 0
} else {
    Write-Host "`n✗ Some validation checks failed. Review output above.`n" -ForegroundColor Red
    exit 1
}

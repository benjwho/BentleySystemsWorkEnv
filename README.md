# INWC_RH Technical Workspace (`NWC_Rehab`)

This repository contains the local Bentley workspace/workset configuration for the `INWC_RH` workspace and `NWC_Rehab` workset.

It is designed for a multi-product Bentley environment with scripted validation and integration checks.

## Scope

Implemented and validated integrations:

- OpenFlows (`FlowMaster`, `WaterCAD`, `WaterGEMS`, `HAMMER`)
- SYNCHRO 4D Pro
- Structural stack (`STAAD`, `AutoPIPE`, `RCDC`, `ADINA`, ISM toggles)
- Geotechnical (`PLAXIS LE`)
- OpenPlant
- OpenCities Map Ultimate
- Bentley Descartes
- iTwin Capture Manage and Extract
- ProjectWise Drive (local-drive mode)
- Python automation (Bentley PowerPlatformPython)

## Top-Level Structure

- `Configuration/`: workspace, workset, organization, standards, automation scripts
- `Projects/`: project runtime folders (`NWC_Rehab`)
- `Data/`: discipline/tool data roots
- `Deliverables/`: export/deliverable roots
- `Logs/`: validation and smoke-check logs
- `_Backups/`: retained safety backups and cleanup archive
- `.vscode/`: local editor settings/tasks/launch for Bentley Python

## Key Entry Points

- Workspace setup: `Configuration/WorkSpaceSetup.cfg`
- Workspace definition: `Configuration/WorkSpaces/INWC_RH.cfg`
- Workset definition: `Configuration/WorkSets/NWC_Rehab.cfg`
- Project org config: `Configuration/Organization/ByProject/NWC_Rehab/NWC_Rehab_Organization.cfg`
- Interop toggles: `Configuration/Organization/Interoperability/Interoperability.cfg`

## Golden Path Runbook

Run from repository root (`C:\Users\zohar\Documents\INWC_RH\tech`).

1. Validate (read-only)

```powershell
& '.\Configuration\Automation\PowerShell\INWC.EnvCheck.ps1'
& '.\Configuration\Automation\PowerShell\INWC.WorkspacePickerFix.ps1'
& '.\Configuration\Automation\PowerShell\INWC.IntegrationCheck.ps1'
```

2. Fix picker enumeration only if needed (writes `WorkSpaceSetup.cfg` only)

```powershell
& '.\Configuration\Automation\PowerShell\INWC.WorkspacePickerFix.ps1' -Fix
& '.\Configuration\Automation\PowerShell\INWC.EnvCheck.ps1'
```

3. Repair ProgramData pointers (optional)

```powershell
& '.\Configuration\Automation\PowerShell\INWC.Repair.ps1' -WhatIf
& '.\Configuration\Automation\PowerShell\INWC.Repair.ps1'
```

4. Reset and rebuild (last resort; reversible via backups/renames)

```powershell
& '.\Configuration\Automation\PowerShell\INWC.ResetRebuild.ps1' -WhatIf -ResetUserPrefs
& '.\Configuration\Automation\PowerShell\INWC.ResetRebuild.ps1' -ResetUserPrefs
```

5. UI picker audit automation (launch + screenshot + optional diagnostics)

```powershell
# Interactive: launches apps, captures screenshots, then asks whether to export diagnostics
& '.\Configuration\Automation\PowerShell\INWC.PickerAudit.ps1'

# Interactive with explicit 20s to set workspace/workset and open a file before each screenshot
& '.\Configuration\Automation\PowerShell\INWC.PickerAudit.ps1' -UserActionSeconds 20

# Interactive and fully manual timing: press Enter when ready to capture each screenshot
& '.\Configuration\Automation\PowerShell\INWC.PickerAudit.ps1' -WaitForEnterBeforeCapture

# Safer autopilot: launches each app with the DGN path as a process argument (no SendKeys)
& '.\Configuration\Automation\PowerShell\INWC.PickerAudit.ps1' -AutoPilot

# Autopilot with explicit file target
& '.\Configuration\Automation\PowerShell\INWC.PickerAudit.ps1' -AutoPilot -AutoPilotFilePath 'C:\Users\zohar\Documents\INWC_RH\tech\Configuration\Standards\Seed\NWC_Rehab_Seed.dgn'

# Legacy fallback (less reliable): SendKeys injection
& '.\Configuration\Automation\PowerShell\INWC.PickerAudit.ps1' -AutoPilot -AutoPilotUseSendKeys

# Non-interactive: force diagnostics export (use this when picker failed)
& '.\Configuration\Automation\PowerShell\INWC.PickerAudit.ps1' -NonInteractive -PickerFailed
```

## Canonical C# CLI

The canonical implementation is now `inwc-cli` at:

- `Configuration/Automation/DotNet/INWC.Automation.Cli/INWC.Automation.Cli.csproj`

PowerShell script entrypoints remain at the same paths and now act as thin compatibility shims.
Legacy script bodies are kept under:

- `Configuration/Automation/PowerShell/Legacy/`

Direct CLI examples:

```powershell
# Use built binary (preferred once built)
.\Configuration\Automation\DotNet\INWC.Automation.Cli\bin\Release\net8.0-windows\win-x64\publish\inwc-cli.exe check env --tech-root .

# Or run from source
dotnet run --project .\Configuration\Automation\DotNet\INWC.Automation.Cli\INWC.Automation.Cli.csproj -- check integration --tech-root .

# Mutating commands support dry-run
.\Configuration\Automation\DotNet\INWC.Automation.Cli\bin\Release\net8.0-windows\win-x64\publish\inwc-cli.exe fix picker-roots --apply --what-if --tech-root .
```

## RuntimeBridge (Primary Runtime Orchestration)

Runtime rules file:

- `Configuration/Automation/Runtime/runtime-rules.json`

Runtime state/artifacts:

- `Logs/NWC_Rehab/RuntimeBridge/queue/*`
- `Logs/NWC_Rehab/RuntimeBridge/runs/*`
- `Logs/NWC_Rehab/RuntimeBridge/events/*.jsonl`
- `C:\ProgramData\INWC\RuntimeBridge\service-state\*`

Core runtime commands:

```powershell
# Start user-session runtime agent (single poll cycle for smoke test)
dotnet run --project .\Configuration\Automation\DotNet\INWC.Automation.Cli\INWC.Automation.Cli.csproj -- runtime agent start --once --poll-seconds 1 --tech-root .

# Trigger a synthetic event and inspect queue
dotnet run --project .\Configuration\Automation\DotNet\INWC.Automation.Cli\INWC.Automation.Cli.csproj -- runtime trigger app.started --tech-root .
dotnet run --project .\Configuration\Automation\DotNet\INWC.Automation.Cli\INWC.Automation.Cli.csproj -- runtime queue list --tech-root . --json

# Manual approval gate for mutating actions
dotnet run --project .\Configuration\Automation\DotNet\INWC.Automation.Cli\INWC.Automation.Cli.csproj -- runtime approve <action-id> --tech-root .
dotnet run --project .\Configuration\Automation\DotNet\INWC.Automation.Cli\INWC.Automation.Cli.csproj -- runtime reject <action-id> --tech-root .
```

## Safety Model

- Validation scripts are read-only by default.
- Write scripts support `-WhatIf` via `ShouldProcess`.
- ProgramData edits always create timestamped `*.bak.<timestamp>` files before modification.
- Reset operations do not delete by default; they rename folders to `*.PURGED.<timestamp>`.
- Reset backups are written under `Logs/RESET_BACKUP_<timestamp>/`.

## Full Health / Smoke

```powershell
& '.\Configuration\Automation\PowerShell\INWC.PythonSmoke.ps1'
& '.\Configuration\Automation\PowerShell\INWC.ProjectWiseSmoke.ps1'
& '.\Configuration\Automation\PowerShell\INWC.FullHealthCheck.ps1'
```

Current known-good baseline log:

- `Logs/NWC_Rehab/FullHealthCheck_20260210-030748-752.txt`

## Cleanup Policy

Current cleanup keeps the latest 2 logs per check type under `Logs/NWC_Rehab` and archives older entries to:

- `_Backups/cleanup_archive/<timestamp>/`

This reduces redundancy while keeping traceability.

## VS Code Setup

Configured interpreter path:

- `C:\ProgramData\Bentley\PowerPlatformPython\python\python.exe`

Files:

- `.vscode/settings.json`
- `.vscode/tasks.json`
- `.vscode/launch.json`

## Seed Files

The project seed files were corrected to valid Bentley DGN binaries:

- `Configuration/Standards/Seed/NWC_Rehab_Seed.dgn`
- `Configuration/Standards/Seed/Civil_Seed.dgn`

Both were replaced from a valid OpenRoads seed source.

## Placeholder Assets (To Be Authored)

The following referenced standards/resources currently exist as placeholders and should be replaced with project-approved content:

- `Configuration/Organization/ByDiscipline/Civil/Civil_Colors.tbl`
- `Configuration/Organization/ByDiscipline/Civil/Civil_PlotStyles.cfg`
- `Configuration/Organization/ByProject/NWC_Rehab/Colors.tbl`
- `Configuration/Organization/ByProject/NWC_Rehab/PlotStyles.cfg`
- `Configuration/Organization/ByProject/NWC_Rehab/Reports_Config.cfg`
- `Configuration/Standards/Cells/Civil_Cells.cel`
- `Configuration/Standards/Cells/NWC_Rehab_Cells.cel`
- `Configuration/Standards/Dgnlib/Civil_Symbols.dgn`
- `Configuration/Standards/Fonts/NWC_Rehab_Fonts.rsc`
- `Configuration/Standards/LineStyles/NWC_Rehab_LineStyles.rsc`
- `Configuration/Standards/Reports/Civil_Reports.cfg`

## GitHub Publish Status

Remote repository:

- `https://github.com/benjwho/BentleySystemsWorkEnv.git`

Local `main` tracks `origin/main`.

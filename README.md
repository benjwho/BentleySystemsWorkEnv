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

## Validation Commands

Run from repository root:

```powershell
& 'Configuration/Automation/PowerShell/INWC.EnvCheck.ps1'
& 'Configuration/Automation/PowerShell/INWC.IntegrationCheck.ps1'
& 'Configuration/Automation/PowerShell/INWC.PythonSmoke.ps1'
& 'Configuration/Automation/PowerShell/INWC.ProjectWiseSmoke.ps1'
& 'Configuration/Automation/PowerShell/INWC.FullHealthCheck.ps1'
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

Local repository can be fully prepared and committed from this workspace.  
Automatic GitHub repo creation was not possible in this environment because GitHub CLI (`gh`) is not installed and no GitHub credentials/token were available.

To publish once you create an empty GitHub repository:

```powershell
git init
git add .
git commit -m "Initial INWC_RH workspace baseline"
git branch -M main
git remote add origin https://github.com/<YOUR_USER>/<YOUR_REPO>.git
git push -u origin main
```


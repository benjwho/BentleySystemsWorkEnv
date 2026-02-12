# AGENTS

This repo is a Bentley MicroStation workspace/workset configuration with automation tooling.

## Current Layout

- Configuration/: MicroStation configuration root (Organization, WorkSpaces, WorkSets, Standards).
- Automation/: C# CLI and PowerShell scripts (moved from Configuration/Automation).
- Projects/, Data/, Deliverables/: project data roots.

## Key Files

- Configuration/WorkSpaceSetup.cfg
- Configuration/WorkSpaces/INWC_RH.cfg
- Configuration/WorkSets/NWC_Rehab.cfg
- Configuration/Organization/ByProject/NWC_Rehab/NWC_Rehab_Organization.cfg
- Automation/DotNet/INWC.Automation.Cli/INWC.Automation.Cli.csproj
- Automation/PowerShell/

## Repo Conventions

- Keep Bentley configuration content under Configuration/.
- Keep automation tooling under Automation/.
- Logs are not retained in the repo.
- inventory.yaml lives in .context and excludes hidden folders.

## Recent Changes

- Logs/ folder removed.
- Configuration/Automation moved to Automation/.
- Paths updated in tech.sln, .vscode/tasks.json, .gitignore, README, and PowerShell scripts.

## Checkpoints

- If you add automation scripts, reference Automation/ paths.
- If you change configuration paths, update WorkSpaceSetup.cfg and related config vars.


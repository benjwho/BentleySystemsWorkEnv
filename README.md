# INWC_RH Technical Workspace

Local Bentley workspace/workset configuration for `INWC_RH` and `NWC_Rehab`.

## Contents

- `Configuration/`: workspace, workset, organization, standards
- `Automation/`: CLI and PowerShell tooling
- `Projects/`: project runtime roots
- `Data/`: tool data roots
- `Deliverables/`: exports/deliverables

## Key Files

- `Configuration/WorkSpaceSetup.cfg`
- `Configuration/WorkSpaces/INWC_RH.cfg`
- `Configuration/WorkSets/NWC_Rehab.cfg`
- `Configuration/Organization/ByProject/NWC_Rehab/NWC_Rehab_Organization.cfg`

## Automation

- Canonical CLI: `Automation/DotNet/INWC.Automation.Cli/INWC.Automation.Cli.csproj`
- PowerShell entrypoints remain under `Automation/PowerShell/`

## Notes

- Logs are generated as needed and are not retained in the repo.

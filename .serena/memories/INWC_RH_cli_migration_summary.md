# INWC_RH CLI migration (2026-02-10)

## Current direction
- Canonical automation now in C# CLI: `Configuration/Automation/DotNet/INWC.Automation.Cli` (assembly `inwc-cli`, net8.0-windows).
- PowerShell entrypoints are thin shims that call `INWC.CliShim.ps1` -> `Invoke-INWCCli`.
- Legacy PowerShell script bodies preserved under `Configuration/Automation/PowerShell/Legacy/`.
- README updated with CLI-first runbook and runtime bridge guidance.
- VS Code tasks updated with CLI tasks.

## Commit info
- Commit on `main`: `ba2f2c8` ("Add INWC CLI and PowerShell shims").
- Includes 102 files changed; CLI tree added; shims + legacy scripts added; README/tasks updated.

## Key files
- CLI project: `Configuration/Automation/DotNet/INWC.Automation.Cli/INWC.Automation.Cli.csproj`
- Entry: `Configuration/Automation/DotNet/INWC.Automation.Cli/Program.cs`
- Shim: `Configuration/Automation/PowerShell/INWC.CliShim.ps1`
- Example shimmed scripts: `INWC.EnvCheck.ps1`, `INWC.IntegrationCheck.ps1`, `INWC.FullHealthCheck.ps1`, `Initialize-INWC-Workspace.ps1`, `INWC.ProjectWiseSmoke.ps1`, `INWC.PythonSmoke.ps1`, `INWC.Repair.ps1`, `INWC.ResetRebuild.ps1`, `INWC.WorkspacePickerFix.ps1`, `INWC.PickerAudit.ps1`, `INWC.BackupRestore.ps1`.

## Gitignore and tasks changes
- `.gitignore` now ignores `Logs/` broadly and DotNet `bin/obj` under CLI.
- `.vscode/tasks.json` includes CLI check env and full health tasks.

## Docs exploration
- Bentley docs visited: https://docs.bentley.com/ -> MicroStation Help 2025.0.1.
- Navigation path: MicroStation Help -> Programmed Customizations -> MDL Applications.
- No auth gates encountered so far.

## Environment notes
- Workspace: `C:\Users\zohar\Documents\INWC_RH\tech` on Windows.
- Git `main` tracks `origin/main`.

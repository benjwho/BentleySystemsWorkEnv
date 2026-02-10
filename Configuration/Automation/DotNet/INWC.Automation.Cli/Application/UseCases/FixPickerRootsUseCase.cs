using System.Text;
using INWC.Automation.Cli.Application.Interfaces;
using INWC.Automation.Cli.Compatibility;
using INWC.Automation.Cli.Domain.Models;
using INWC.Automation.Cli.Infrastructure.Config;
using INWC.Automation.Cli.Infrastructure.System;

namespace INWC.Automation.Cli.Application.UseCases;

internal sealed class FixPickerRootsUseCase : ICommandUseCase<FixPickerRootsOptions, CommandResult>
{
    private readonly IConfigFileReader _configReader;
    private readonly IExitCodePolicy _exitCodePolicy;

    public FixPickerRootsUseCase(IConfigFileReader configReader, IExitCodePolicy exitCodePolicy)
    {
        _configReader = configReader;
        _exitCodePolicy = exitCodePolicy;
    }

    public CommandResult Execute(CommandContext context, FixPickerRootsOptions options)
    {
        var checks = new List<CheckRecord>();
        var techRoot = context.Global.TechRoot;
        var cfgRoot = Path.Combine(techRoot, "Configuration");
        var workSpaceSetupPath = Path.Combine(cfgRoot, "WorkSpaceSetup.cfg");
        var workSpacesDir = Path.Combine(cfgRoot, "WorkSpaces");
        var workSetsDir = Path.Combine(cfgRoot, "WorkSets");

        var expectedWorkSpacesRoot = _configReader.NormalizeCfgRoot(workSpacesDir);
        var expectedWorkSetsRoot = _configReader.NormalizeCfgRoot(workSetsDir);

        CheckHelpers.Add(checks, "Local", "WorkSpaceSetup exists", workSpaceSetupPath, File.Exists(workSpaceSetupPath));
        CheckHelpers.Add(checks, "Local", "WorkSpaces folder exists", workSpacesDir, Directory.Exists(workSpacesDir));
        CheckHelpers.Add(checks, "Local", "WorkSets folder exists", workSetsDir, Directory.Exists(workSetsDir));

        string? currentMyWorkSpaces = null;
        string? currentMyWorkSets = null;
        string? currentUstnWorkSpaces = null;
        string? currentUstnWorkSets = null;

        if (File.Exists(workSpaceSetupPath))
        {
            currentMyWorkSpaces = _configReader.GetCfgVarValue(workSpaceSetupPath, "MY_WORKSPACES_LOCATION");
            currentMyWorkSets = _configReader.GetCfgVarValue(workSpaceSetupPath, "MY_WORKSET_LOCATION");
            currentUstnWorkSpaces = _configReader.GetCfgVarValue(workSpaceSetupPath, "_USTN_WORKSPACESROOT");
            currentUstnWorkSets = _configReader.GetCfgVarValue(workSpaceSetupPath, "_USTN_WORKSETSROOT");

            CheckHelpers.Add(
                checks,
                "WorkSpaceSetup",
                "MY_WORKSPACES_LOCATION",
                expectedWorkSpacesRoot ?? string.Empty,
                string.Equals(_configReader.NormalizeCfgRoot(currentMyWorkSpaces), expectedWorkSpacesRoot, StringComparison.OrdinalIgnoreCase),
                currentMyWorkSpaces ?? string.Empty);

            CheckHelpers.Add(
                checks,
                "WorkSpaceSetup",
                "MY_WORKSET_LOCATION",
                expectedWorkSetsRoot ?? string.Empty,
                string.Equals(_configReader.NormalizeCfgRoot(currentMyWorkSets), expectedWorkSetsRoot, StringComparison.OrdinalIgnoreCase),
                currentMyWorkSets ?? string.Empty);

            var wsRootOk = string.Equals(currentUstnWorkSpaces, "$(MY_WORKSPACES_LOCATION)", StringComparison.Ordinal)
                           || string.Equals(_configReader.NormalizeCfgRoot(currentUstnWorkSpaces), expectedWorkSpacesRoot, StringComparison.OrdinalIgnoreCase);

            var wsetRootOk = string.Equals(currentUstnWorkSets, "$(MY_WORKSET_LOCATION)", StringComparison.Ordinal)
                             || string.Equals(_configReader.NormalizeCfgRoot(currentUstnWorkSets), expectedWorkSetsRoot, StringComparison.OrdinalIgnoreCase);

            CheckHelpers.Add(checks, "WorkSpaceSetup", "_USTN_WORKSPACESROOT", expectedWorkSpacesRoot ?? string.Empty, wsRootOk, currentUstnWorkSpaces ?? string.Empty);
            CheckHelpers.Add(checks, "WorkSpaceSetup", "_USTN_WORKSETSROOT", expectedWorkSetsRoot ?? string.Empty, wsetRootOk, currentUstnWorkSets ?? string.Empty);
        }

        var needsFix = CheckHelpers.HasFailure(checks);

        if (!options.Apply)
        {
            return new CommandResult
            {
                ExitCode = needsFix ? _exitCodePolicy.NeedsFix : _exitCodePolicy.Success,
                Message = needsFix
                    ? "CHECK: picker variables need normalization. Re-run with --apply to apply changes."
                    : "CHECK: picker variables are already valid.",
                Checks = checks
            };
        }

        if (!File.Exists(workSpaceSetupPath))
        {
            return new CommandResult
            {
                ExitCode = _exitCodePolicy.Failure,
                Message = "FIX: WorkSpaceSetup.cfg is missing. Cannot apply fix.",
                Checks = checks
            };
        }

        if (!needsFix)
        {
            return new CommandResult
            {
                ExitCode = _exitCodePolicy.Success,
                Message = "No change: WorkSpaceSetup.cfg already has correct picker roots.",
                Checks = checks
            };
        }

        var managedBlock = string.Join(
            "\r\n",
            "# --- BEGIN INWC_PICKER_ROOTS (managed) ---",
            $"MY_WORKSPACES_LOCATION = {expectedWorkSpacesRoot}",
            $"MY_WORKSET_LOCATION = {expectedWorkSetsRoot}",
            "_USTN_WORKSPACESROOT = $(MY_WORKSPACES_LOCATION)",
            "_USTN_WORKSETSROOT = $(MY_WORKSET_LOCATION)",
            "# --- END INWC_PICKER_ROOTS (managed) ---");

        var originalText = File.ReadAllText(workSpaceSetupPath);
        var updatedText = TextFileMutator.SetManagedPickerBlock(originalText, managedBlock);
        if (string.Equals(originalText, updatedText, StringComparison.Ordinal))
        {
            return new CommandResult
            {
                ExitCode = _exitCodePolicy.Success,
                Message = "No change: WorkSpaceSetup.cfg already matches managed picker block.",
                Checks = checks
            };
        }

        var artifacts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (context.Global.WhatIf)
        {
            artifacts["PlannedUpdate"] = workSpaceSetupPath;
            return new CommandResult
            {
                ExitCode = _exitCodePolicy.Success,
                Message = "WHAT-IF: would backup and normalize picker roots in WorkSpaceSetup.cfg.",
                Checks = checks,
                Artifacts = artifacts
            };
        }

        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var backupPath = workSpaceSetupPath + ".bak." + stamp;
        File.Copy(workSpaceSetupPath, backupPath, true);
        File.WriteAllText(workSpaceSetupPath, updatedText, Encoding.ASCII);

        artifacts["Backup"] = backupPath;
        artifacts["UpdatedFile"] = workSpaceSetupPath;

        return new CommandResult
        {
            ExitCode = _exitCodePolicy.Success,
            Message = "FIXED: WorkSpaceSetup.cfg picker roots normalized.",
            Checks = checks,
            Artifacts = artifacts
        };
    }
}

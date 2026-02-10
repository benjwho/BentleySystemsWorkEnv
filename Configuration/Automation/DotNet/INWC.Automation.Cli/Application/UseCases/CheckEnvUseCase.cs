using INWC.Automation.Cli.Application.Interfaces;
using INWC.Automation.Cli.Compatibility;
using INWC.Automation.Cli.Domain.Models;
using INWC.Automation.Cli.Infrastructure.Config;

namespace INWC.Automation.Cli.Application.UseCases;

internal sealed class CheckEnvUseCase : ICommandUseCase<CheckEnvOptions, CommandResult>
{
    private readonly IConfigFileReader _configReader;
    private readonly IExitCodePolicy _exitCodePolicy;

    public CheckEnvUseCase(IConfigFileReader configReader, IExitCodePolicy exitCodePolicy)
    {
        _configReader = configReader;
        _exitCodePolicy = exitCodePolicy;
    }

    public CommandResult Execute(CommandContext context, CheckEnvOptions options)
    {
        var checks = new List<CheckRecord>();
        var techRoot = context.Global.TechRoot;

        var cfgRoot = Path.Combine(techRoot, "Configuration");
        var workSpaceSetupPath = Path.Combine(cfgRoot, "WorkSpaceSetup.cfg");
        var workSpacesCfg = Path.Combine(cfgRoot, "WorkSpaces", "INWC_RH.cfg");
        var workSetsCfg = Path.Combine(cfgRoot, "WorkSets", "NWC_Rehab.cfg");

        var expectedCfgRoot = _configReader.NormalizeCfgRoot(cfgRoot);
        var expectedWorkSpacesRoot = _configReader.NormalizeCfgRoot(Path.Combine(cfgRoot, "WorkSpaces"));
        var expectedWorkSetsRoot = _configReader.NormalizeCfgRoot(Path.Combine(cfgRoot, "WorkSets"));

        var requiredPaths = new[]
        {
            techRoot,
            cfgRoot,
            workSpaceSetupPath,
            workSpacesCfg,
            workSetsCfg
        };

        foreach (var path in requiredPaths)
        {
            CheckHelpers.Add(checks, "Local", "Path exists", path, File.Exists(path) || Directory.Exists(path));
        }

        if (File.Exists(workSpaceSetupPath))
        {
            var myWorkSpaces = _configReader.GetCfgVarValue(workSpaceSetupPath, "MY_WORKSPACES_LOCATION");
            var myWorkSets = _configReader.GetCfgVarValue(workSpaceSetupPath, "MY_WORKSET_LOCATION");
            var ustnWorkSpaces = _configReader.GetCfgVarValue(workSpaceSetupPath, "_USTN_WORKSPACESROOT");
            var ustnWorkSets = _configReader.GetCfgVarValue(workSpaceSetupPath, "_USTN_WORKSETSROOT");

            var myWorkSpacesNorm = _configReader.NormalizeCfgRoot(myWorkSpaces);
            var myWorkSetsNorm = _configReader.NormalizeCfgRoot(myWorkSets);

            CheckHelpers.Add(
                checks,
                "WorkSpaceSetup",
                "MY_WORKSPACES_LOCATION normalized",
                expectedWorkSpacesRoot ?? string.Empty,
                string.Equals(myWorkSpacesNorm, expectedWorkSpacesRoot, StringComparison.OrdinalIgnoreCase),
                myWorkSpaces ?? string.Empty);

            CheckHelpers.Add(
                checks,
                "WorkSpaceSetup",
                "MY_WORKSET_LOCATION normalized",
                expectedWorkSetsRoot ?? string.Empty,
                string.Equals(myWorkSetsNorm, expectedWorkSetsRoot, StringComparison.OrdinalIgnoreCase),
                myWorkSets ?? string.Empty);

            var wsRootOk = string.Equals(ustnWorkSpaces, "$(MY_WORKSPACES_LOCATION)", StringComparison.Ordinal)
                           || string.Equals(_configReader.NormalizeCfgRoot(ustnWorkSpaces), expectedWorkSpacesRoot, StringComparison.OrdinalIgnoreCase);

            var wsetRootOk = string.Equals(ustnWorkSets, "$(MY_WORKSET_LOCATION)", StringComparison.Ordinal)
                             || string.Equals(_configReader.NormalizeCfgRoot(ustnWorkSets), expectedWorkSetsRoot, StringComparison.OrdinalIgnoreCase);

            CheckHelpers.Add(checks, "WorkSpaceSetup", "_USTN_WORKSPACESROOT resolves", expectedWorkSpacesRoot ?? string.Empty, wsRootOk, ustnWorkSpaces ?? string.Empty);
            CheckHelpers.Add(checks, "WorkSpaceSetup", "_USTN_WORKSETSROOT resolves", expectedWorkSetsRoot ?? string.Empty, wsetRootOk, ustnWorkSets ?? string.Empty);
        }

        var userEnvCustom = _configReader.NormalizeCfgRoot(Environment.GetEnvironmentVariable("_USTN_CUSTOM_CONFIGURATION", EnvironmentVariableTarget.User));
        var machineEnvCustom = _configReader.NormalizeCfgRoot(Environment.GetEnvironmentVariable("_USTN_CUSTOM_CONFIGURATION", EnvironmentVariableTarget.Machine));
        var envPointerOk = string.Equals(userEnvCustom, expectedCfgRoot, StringComparison.OrdinalIgnoreCase)
                           || string.Equals(machineEnvCustom, expectedCfgRoot, StringComparison.OrdinalIgnoreCase);

        CheckHelpers.Add(
            checks,
            "Environment",
            "User/Machine _USTN_CUSTOM_CONFIGURATION",
            expectedCfgRoot ?? string.Empty,
            envPointerOk,
            $"User={userEnvCustom}; Machine={machineEnvCustom}");

        foreach (var productRoot in options.ProductConfigRoots)
        {
            var setupCfg = Path.Combine(productRoot, "ConfigurationSetup.cfg");
            var setupExists = File.Exists(setupCfg);
            CheckHelpers.Add(checks, "ProgramData", "ConfigurationSetup exists", setupCfg, setupExists);

            if (!setupExists)
            {
                continue;
            }

            var rawCustom = _configReader.GetCfgVarValue(setupCfg, "_USTN_CUSTOM_CONFIGURATION");
            var rawCustomNorm = _configReader.NormalizeCfgRoot(rawCustom);
            var pointerOk = string.Equals(rawCustomNorm, expectedCfgRoot, StringComparison.OrdinalIgnoreCase)
                            || (string.IsNullOrWhiteSpace(rawCustomNorm) && envPointerOk);

            CheckHelpers.Add(
                checks,
                "ProgramData",
                "_USTN_CUSTOM_CONFIGURATION points to INWC cfg",
                productRoot,
                pointerOk,
                $"Config={rawCustomNorm}; EnvFallback={envPointerOk}");

            var effectivePointer = !string.IsNullOrWhiteSpace(rawCustomNorm)
                ? rawCustomNorm
                : envPointerOk ? expectedCfgRoot : null;

            var computedInclude = !string.IsNullOrWhiteSpace(effectivePointer)
                ? effectivePointer!.Replace('/', '\\') + "WorkSpaceSetup.cfg"
                : string.Empty;

            var includeOk = !string.IsNullOrWhiteSpace(computedInclude) && File.Exists(computedInclude);
            CheckHelpers.Add(
                checks,
                "ProgramData",
                "Computed include resolves WorkSpaceSetup.cfg",
                computedInclude,
                includeOk,
                "Bentley include: $(_USTN_CONFIGURATION)WorkSpaceSetup.cfg");
        }

        var hasFailure = CheckHelpers.HasFailure(checks);
        return new CommandResult
        {
            ExitCode = hasFailure ? _exitCodePolicy.Failure : _exitCodePolicy.Success,
            Message = hasFailure ? "FAIL: environment checks found issues." : "PASS: environment checks passed.",
            Checks = checks
        };
    }
}

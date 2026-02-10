using System.Text.RegularExpressions;
using INWC.Automation.Cli.Application.Interfaces;
using INWC.Automation.Cli.Compatibility;
using INWC.Automation.Cli.Domain.Models;
using INWC.Automation.Cli.Infrastructure.Config;

namespace INWC.Automation.Cli.Application.UseCases;

internal sealed class CheckIntegrationUseCase : ICommandUseCase<CheckIntegrationOptions, CommandResult>
{
    private readonly IConfigFileReader _configReader;
    private readonly IExitCodePolicy _exitCodePolicy;

    public CheckIntegrationUseCase(IConfigFileReader configReader, IExitCodePolicy exitCodePolicy)
    {
        _configReader = configReader;
        _exitCodePolicy = exitCodePolicy;
    }

    public CommandResult Execute(CommandContext context, CheckIntegrationOptions options)
    {
        var checks = new List<CheckRecord>();

        var techRoot = context.Global.TechRoot;
        var cfgRoot = Path.Combine(techRoot, "Configuration");
        var projectRoot = Path.Combine(techRoot, "Projects", "NWC_Rehab");

        var tokenMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["INWC_TECH"] = techRoot,
            ["INWC_CFG"] = cfgRoot,
            ["INWC_PROJECT_ROOT"] = projectRoot,
            ["INWC_EXPORT"] = Path.Combine(projectRoot, "Exports"),
            ["INWC_DATA"] = Path.Combine(techRoot, "Data"),
            ["INWC_LOGS"] = Path.Combine(techRoot, "Logs")
        };

        var orgCfg = Path.Combine(cfgRoot, "Organization", "ByProject", "NWC_Rehab", "NWC_Rehab_Organization.cfg");
        var interopCfg = Path.Combine(cfgRoot, "Organization", "Interoperability", "Interoperability.cfg");

        CheckHelpers.Add(checks, "Config", "Project organization config exists", orgCfg, File.Exists(orgCfg));
        CheckHelpers.Add(checks, "Config", "Interoperability config exists", interopCfg, File.Exists(interopCfg));

        if (!File.Exists(orgCfg) || !File.Exists(interopCfg))
        {
            return new CommandResult
            {
                ExitCode = _exitCodePolicy.Failure,
                Message = "FAIL: required config files are missing.",
                Checks = checks
            };
        }

        var orgText = File.ReadAllText(orgCfg);

        var includeVars = new[]
        {
            "INTEROPERABILITY_CONFIG",
            "DISCIPLINE_CIVIL_CONFIG",
            "DISCIPLINE_STRUCTURES_CONFIG",
            "DISCIPLINE_GEOTECHNICAL_CONFIG",
            "DISCIPLINE_UTILITIES_CONFIG",
            "DISCIPLINE_PLANT_CONFIG",
            "FUNCTION_SCHEDULING_CONFIG",
            "FUNCTION_GEOSPATIAL_CONFIG",
            "FUNCTION_REALITY_DESCARTES_CONFIG",
            "FUNCTION_REALITY_ITWIN_CAPTURE_CONFIG",
            "FUNCTION_DOCUMENT_MANAGEMENT_CONFIG",
            "FUNCTION_AUTOMATION_CONFIG"
        };

        var resolvedIncludePaths = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var variable in includeVars)
        {
            var raw = _configReader.GetCfgVarValue(orgCfg, variable);
            var resolved = _configReader.ResolveCfgTokens(raw, tokenMap);
            var exists = !string.IsNullOrWhiteSpace(resolved) && File.Exists(resolved);

            CheckHelpers.Add(checks, "IncludeVar", variable, resolved ?? string.Empty, exists, raw ?? string.Empty);

            var includePattern = "(?m)^\\s*%include\\s+\\$\\(" + Regex.Escape(variable) + "\\)\\s*$";
            CheckHelpers.Add(
                checks,
                "IncludeLine",
                $"%include $({variable})",
                orgCfg,
                Regex.IsMatch(orgText, includePattern),
                string.Empty);

            resolvedIncludePaths[variable] = resolved;
        }

        var toggleVars = new[]
        {
            "INWC_INTEROP_OPENFLOWS",
            "INWC_INTEROP_SYNCHRO",
            "INWC_INTEROP_STRUCTURAL",
            "INWC_INTEROP_GEOTECH",
            "INWC_INTEROP_OPENPLANT",
            "INWC_INTEROP_OPENCITIES",
            "INWC_INTEROP_DESCARTES",
            "INWC_INTEROP_ITWIN_CAPTURE",
            "INWC_INTEROP_PROJECTWISE_DRIVE",
            "INWC_INTEROP_PYTHON_AUTOMATION",
            "INWC_INTEROP_ISM_SYNC",
            "INWC_INTEROP_WATERCAD_DGN_LINK",
            "INWC_INTEROP_HAMMER_DGN_LINK",
            "INWC_INTEROP_SCHEDULE_LINK"
        };

        var toggleValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var toggle in toggleVars)
        {
            var value = _configReader.GetCfgVarValue(interopCfg, toggle);
            toggleValues[toggle] = value;
            CheckHelpers.Add(
                checks,
                "Toggle",
                toggle,
                interopCfg,
                string.Equals(value, "0", StringComparison.Ordinal) || string.Equals(value, "1", StringComparison.Ordinal),
                value ?? string.Empty);
        }

        var utilCfg = resolvedIncludePaths.GetValueOrDefault("DISCIPLINE_UTILITIES_CONFIG");
        var structCfg = resolvedIncludePaths.GetValueOrDefault("DISCIPLINE_STRUCTURES_CONFIG");
        var geotechCfg = resolvedIncludePaths.GetValueOrDefault("DISCIPLINE_GEOTECHNICAL_CONFIG");
        var plantCfg = resolvedIncludePaths.GetValueOrDefault("DISCIPLINE_PLANT_CONFIG");
        var syncCfg = resolvedIncludePaths.GetValueOrDefault("FUNCTION_SCHEDULING_CONFIG");
        var geoCfg = resolvedIncludePaths.GetValueOrDefault("FUNCTION_GEOSPATIAL_CONFIG");
        var descCfg = resolvedIncludePaths.GetValueOrDefault("FUNCTION_REALITY_DESCARTES_CONFIG");
        var itwinCfg = resolvedIncludePaths.GetValueOrDefault("FUNCTION_REALITY_ITWIN_CAPTURE_CONFIG");
        var docCfg = resolvedIncludePaths.GetValueOrDefault("FUNCTION_DOCUMENT_MANAGEMENT_CONFIG");
        var pythonCfg = resolvedIncludePaths.GetValueOrDefault("FUNCTION_AUTOMATION_CONFIG");

        var executableChecks = new[]
        {
            new ExecutableCheck("FlowMaster", "INWC_INTEROP_OPENFLOWS", utilCfg, "FLOWMASTER_EXE"),
            new ExecutableCheck("WaterCAD", "INWC_INTEROP_OPENFLOWS", utilCfg, "WATERCAD_EXE"),
            new ExecutableCheck("WaterGEMS", "INWC_INTEROP_OPENFLOWS", utilCfg, "WATERGEMS_EXE"),
            new ExecutableCheck("HAMMER", "INWC_INTEROP_OPENFLOWS", utilCfg, "HAMMER_EXE"),
            new ExecutableCheck("SYNCHRO", "INWC_INTEROP_SYNCHRO", syncCfg, "SYNCHRO_EXE_PATH"),
            new ExecutableCheck("STAAD", "INWC_INTEROP_STRUCTURAL", structCfg, "STAAD_EXE"),
            new ExecutableCheck("AutoPIPE", "INWC_INTEROP_STRUCTURAL", structCfg, "AUTOPIPE_EXE"),
            new ExecutableCheck("RCDC", "INWC_INTEROP_STRUCTURAL", structCfg, "RCDC_EXE"),
            new ExecutableCheck("ADINA", "INWC_INTEROP_STRUCTURAL", structCfg, "ADINA_UI_EXE"),
            new ExecutableCheck("PLAXIS LE", "INWC_INTEROP_GEOTECH", geotechCfg, "PLAXIS_LE_EXE"),
            new ExecutableCheck("OpenPlant", "INWC_INTEROP_OPENPLANT", plantCfg, "OPENPLANT_EXE"),
            new ExecutableCheck("OpenPlant IsoExtractor", "INWC_INTEROP_OPENPLANT", plantCfg, "OPENPLANT_ISO_EXTRACTOR_EXE"),
            new ExecutableCheck("OpenCities Map", "INWC_INTEROP_OPENCITIES", geoCfg, "OPENCITIES_EXE"),
            new ExecutableCheck("Descartes", "INWC_INTEROP_DESCARTES", descCfg, "DESCARTES_EXE"),
            new ExecutableCheck("iTwin Capture", "INWC_INTEROP_ITWIN_CAPTURE", itwinCfg, "ITWIN_CAPTURE_EXE"),
            new ExecutableCheck("ProjectWise Drive", "INWC_INTEROP_PROJECTWISE_DRIVE", docCfg, "PROJECTWISE_DRIVE_EXE"),
            new ExecutableCheck("Bentley Python", "INWC_INTEROP_PYTHON_AUTOMATION", pythonCfg, "PYTHON_EXE")
        };

        foreach (var item in executableChecks)
        {
            if (string.IsNullOrWhiteSpace(item.ConfigPath) || !File.Exists(item.ConfigPath))
            {
                CheckHelpers.Add(checks, "Executable", item.Tool, item.ConfigPath ?? string.Empty, false, "Config file missing");
                continue;
            }

            var raw = _configReader.GetCfgVarValue(item.ConfigPath, item.Variable);
            var resolved = _configReader.ResolveCfgTokens(raw, tokenMap);
            var exists = !string.IsNullOrWhiteSpace(resolved) && File.Exists(resolved);

            var toggleValue = toggleValues.GetValueOrDefault(item.Toggle);
            var enabled = string.Equals(toggleValue, "1", StringComparison.Ordinal);
            var ok = enabled ? exists : true;
            var detail = enabled ? "Toggle=1" : "Toggle=0 (check informational)";

            CheckHelpers.Add(
                checks,
                "Executable",
                item.Tool,
                resolved ?? string.Empty,
                ok,
                $"{detail}; Raw={raw}");

        }

        var folderChecks = new[]
        {
            new FolderCheck(Path.Combine(projectRoot, "Analysis", "Water"), "INWC_INTEROP_OPENFLOWS"),
            new FolderCheck(Path.Combine(projectRoot, "Reports", "Hydraulic"), "INWC_INTEROP_OPENFLOWS"),
            new FolderCheck(Path.Combine(projectRoot, "Exports", "SYNCHRO"), "INWC_INTEROP_SYNCHRO"),
            new FolderCheck(Path.Combine(projectRoot, "Analysis", "Structural"), "INWC_INTEROP_STRUCTURAL"),
            new FolderCheck(Path.Combine(projectRoot, "Analysis", "Geotechnical"), "INWC_INTEROP_GEOTECH"),
            new FolderCheck(Path.Combine(projectRoot, "Models", "Plant"), "INWC_INTEROP_OPENPLANT"),
            new FolderCheck(Path.Combine(projectRoot, "Exports", "OpenPlant", "Isometrics"), "INWC_INTEROP_OPENPLANT"),
            new FolderCheck(Path.Combine(projectRoot, "Analysis", "Geospatial"), "INWC_INTEROP_OPENCITIES"),
            new FolderCheck(Path.Combine(projectRoot, "Exports", "OpenCities"), "INWC_INTEROP_OPENCITIES"),
            new FolderCheck(Path.Combine(projectRoot, "Analysis", "Reality", "RasterProcessed"), "INWC_INTEROP_DESCARTES"),
            new FolderCheck(Path.Combine(projectRoot, "Exports", "iTwinCapture"), "INWC_INTEROP_ITWIN_CAPTURE"),
            new FolderCheck(Path.Combine(projectRoot, "DocumentManagement", "ProjectWiseDrive"), "INWC_INTEROP_PROJECTWISE_DRIVE"),
            new FolderCheck(Path.Combine(cfgRoot, "Automation", "Python"), "INWC_INTEROP_PYTHON_AUTOMATION")
        };

        foreach (var item in folderChecks)
        {
            var toggle = toggleValues.GetValueOrDefault(item.Toggle);
            var enabled = string.Equals(toggle, "1", StringComparison.Ordinal);
            var exists = Directory.Exists(item.Path);
            var ok = enabled ? exists : true;
            var detail = enabled ? "Toggle=1" : "Toggle=0 (check informational)";
            CheckHelpers.Add(checks, "Folder", "Path exists", item.Path, ok, detail);
        }

        var pwEnabledSetting = _configReader.GetCfgVarValue(orgCfg, "PROJECTWISE_ENABLED");
        var pwServerModeOk = !string.Equals(pwEnabledSetting, "1", StringComparison.Ordinal);
        CheckHelpers.Add(checks, "Policy", "ProjectWise server mode not forced", orgCfg, pwServerModeOk, $"PROJECTWISE_ENABLED={pwEnabledSetting}");

        if (!string.IsNullOrWhiteSpace(docCfg) && File.Exists(docCfg))
        {
            var pwDriveEnabled = _configReader.GetCfgVarValue(docCfg, "PROJECTWISE_DRIVE_ENABLED");
            CheckHelpers.Add(
                checks,
                "Policy",
                "PROJECTWISE_DRIVE_ENABLED uses interop toggle",
                docCfg,
                string.Equals(pwDriveEnabled, "$(INWC_INTEROP_PROJECTWISE_DRIVE)", StringComparison.Ordinal),
                pwDriveEnabled ?? string.Empty);
        }

        var hasFailure = CheckHelpers.HasFailure(checks);

        return new CommandResult
        {
            ExitCode = hasFailure ? _exitCodePolicy.Failure : _exitCodePolicy.Success,
            Message = hasFailure ? "FAIL: integration checks found issues." : "PASS: integration checks passed.",
            Checks = checks
        };
    }

    private sealed record ExecutableCheck(string Tool, string Toggle, string? ConfigPath, string Variable);
    private sealed record FolderCheck(string Path, string Toggle);
}

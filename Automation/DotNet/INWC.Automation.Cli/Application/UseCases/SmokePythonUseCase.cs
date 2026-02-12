using System.Text;
using INWC.Automation.Cli.Application.Interfaces;
using INWC.Automation.Cli.Compatibility;
using INWC.Automation.Cli.Domain.Models;
using INWC.Automation.Cli.Infrastructure.Config;
using INWC.Automation.Cli.Infrastructure.Processes;

namespace INWC.Automation.Cli.Application.UseCases;

internal sealed class SmokePythonUseCase : ICommandUseCase<SmokePythonOptions, CommandResult>
{
    private readonly IConfigFileReader _configReader;
    private readonly IArtifactPathPolicy _artifactPathPolicy;
    private readonly IExitCodePolicy _exitCodePolicy;
    private readonly IProcessRunner _processRunner;

    public SmokePythonUseCase(
        IConfigFileReader configReader,
        IArtifactPathPolicy artifactPathPolicy,
        IExitCodePolicy exitCodePolicy,
        IProcessRunner processRunner)
    {
        _configReader = configReader;
        _artifactPathPolicy = artifactPathPolicy;
        _exitCodePolicy = exitCodePolicy;
        _processRunner = processRunner;
    }

    public CommandResult Execute(CommandContext context, SmokePythonOptions options)
    {
        var checks = new List<CheckRecord>();
        var techRoot = context.Global.TechRoot;
        var cfgRoot = Path.Combine(techRoot, "Configuration");
        var projectRoot = Path.Combine(techRoot, "Projects", "NWC_Rehab");

        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["INWC_TECH"] = techRoot,
            ["INWC_CFG"] = cfgRoot,
            ["INWC_PROJECT_ROOT"] = projectRoot,
            ["INWC_EXPORT"] = Path.Combine(projectRoot, "Exports"),
            ["INWC_DATA"] = Path.Combine(techRoot, "Data")
        };

        var orgCfg = Path.Combine(cfgRoot, "Organization", "ByProject", "NWC_Rehab", "NWC_Rehab_Organization.cfg");
        var interopCfg = Path.Combine(cfgRoot, "Organization", "Interoperability", "Interoperability.cfg");
        var pythonCfg = Path.Combine(cfgRoot, "Organization", "ByFunction", "Automation", "Python_Integration.cfg");

        foreach (var filePath in new[] { orgCfg, interopCfg, pythonCfg })
        {
            CheckHelpers.Add(checks, "Config", "File exists", filePath, File.Exists(filePath));
        }

        var automationVarRaw = _configReader.GetCfgVarValue(orgCfg, "FUNCTION_AUTOMATION_CONFIG");
        var automationVarResolved = _configReader.ResolveCfgTokens(automationVarRaw, tokens);
        var automationVarOk = !string.IsNullOrWhiteSpace(automationVarResolved) && File.Exists(automationVarResolved);
        CheckHelpers.Add(checks, "Config", "FUNCTION_AUTOMATION_CONFIG resolves", automationVarResolved ?? string.Empty, automationVarOk, automationVarRaw ?? string.Empty);

        var orgText = File.Exists(orgCfg) ? File.ReadAllText(orgCfg) : string.Empty;
        var hasAutomationInclude = System.Text.RegularExpressions.Regex.IsMatch(
            orgText,
            "(?m)^\\s*%include\\s+\\$\\(FUNCTION_AUTOMATION_CONFIG\\)\\s*$");
        CheckHelpers.Add(checks, "Config", "%include $(FUNCTION_AUTOMATION_CONFIG) present", orgCfg, hasAutomationInclude);

        var toggleRaw = _configReader.GetCfgVarValue(interopCfg, "INWC_INTEROP_PYTHON_AUTOMATION");
        var toggleOk = string.Equals(toggleRaw, "0", StringComparison.Ordinal) || string.Equals(toggleRaw, "1", StringComparison.Ordinal);
        CheckHelpers.Add(checks, "Toggle", "INWC_INTEROP_PYTHON_AUTOMATION is 0/1", interopCfg, toggleOk, toggleRaw ?? string.Empty);

        var enabledRaw = _configReader.GetCfgVarValue(pythonCfg, "PYTHON_AUTOMATION_ENABLED");
        var enabledUsesToggle = string.Equals(enabledRaw, "$(INWC_INTEROP_PYTHON_AUTOMATION)", StringComparison.Ordinal);
        CheckHelpers.Add(checks, "Config", "PYTHON_AUTOMATION_ENABLED uses interoperability toggle", pythonCfg, enabledUsesToggle, enabledRaw ?? string.Empty);

        var pythonExeRaw = _configReader.GetCfgVarValue(pythonCfg, "PYTHON_EXE");
        var pythonExe = _configReader.ResolveCfgTokens(pythonExeRaw, tokens);
        var pythonExeOk = !string.IsNullOrWhiteSpace(pythonExe) && File.Exists(pythonExe);
        CheckHelpers.Add(checks, "Executable", "PYTHON_EXE exists", pythonExe ?? string.Empty, pythonExeOk, pythonExeRaw ?? string.Empty);

        var scriptsDirRaw = _configReader.GetCfgVarValue(pythonCfg, "PYTHON_SCRIPTS_DIR");
        var scriptsDir = _configReader.ResolveCfgTokens(scriptsDirRaw, tokens);
        var scriptsDirOk = !string.IsNullOrWhiteSpace(scriptsDir) && Directory.Exists(scriptsDir);
        CheckHelpers.Add(checks, "Folders", "PYTHON_SCRIPTS_DIR exists", scriptsDir ?? string.Empty, scriptsDirOk, scriptsDirRaw ?? string.Empty);

        var pythonLogDirRaw = _configReader.GetCfgVarValue(pythonCfg, "PYTHON_LOG_DIR");
        var pythonLogDir = _configReader.ResolveCfgTokens(pythonLogDirRaw, tokens);
        var pythonLogDirOk = !string.IsNullOrWhiteSpace(pythonLogDir) && Directory.Exists(pythonLogDir);
        CheckHelpers.Add(checks, "Folders", "PYTHON_LOG_DIR exists", pythonLogDir ?? string.Empty, pythonLogDirOk, pythonLogDirRaw ?? string.Empty);

        if (pythonLogDirOk && !string.IsNullOrWhiteSpace(pythonLogDir))
        {
            var tempFile = Path.Combine(pythonLogDir, "._inwc_python_smoke_" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllText(tempFile, "INWC Python smoke write test", Encoding.ASCII);
                var wrote = File.Exists(tempFile);
                if (wrote)
                {
                    File.Delete(tempFile);
                }

                CheckHelpers.Add(checks, "WriteTest", "PYTHON_LOG_DIR writable", pythonLogDir, wrote);
            }
            catch (Exception ex)
            {
                CheckHelpers.Add(checks, "WriteTest", "PYTHON_LOG_DIR writable", pythonLogDir, false, ex.Message);
                TryDelete(tempFile);
            }
        }
        else
        {
            CheckHelpers.Add(checks, "WriteTest", "PYTHON_LOG_DIR writable", pythonLogDir ?? string.Empty, false, "PYTHON_LOG_DIR missing");
        }

        if (options.SkipRuntimeTest)
        {
            CheckHelpers.Add(checks, "Runtime", "Python runtime smoke", pythonExe ?? string.Empty, true, "Skipped by switch");
        }
        else if (!pythonExeOk || string.IsNullOrWhiteSpace(pythonExe))
        {
            CheckHelpers.Add(checks, "Runtime", "Python runtime smoke", pythonExe ?? string.Empty, false, "PYTHON_EXE missing");
        }
        else
        {
            var versionRun = _processRunner.Run(pythonExe, ["--version"], techRoot);
            var versionDetail = (versionRun.StdOut + versionRun.StdErr).Trim();
            CheckHelpers.Add(checks, "Runtime", "python --version exits 0", pythonExe, versionRun.ExitCode == 0, versionDetail);

            var smokeRun = _processRunner.Run(pythonExe, ["-c", "import platform; print('INWC_PYTHON_SMOKE_OK|' + platform.python_version())"], techRoot);
            var smokeDetail = (smokeRun.StdOut + smokeRun.StdErr).Trim();
            var smokeOk = smokeRun.ExitCode == 0 && smokeDetail.Contains("INWC_PYTHON_SMOKE_OK|", StringComparison.Ordinal);
            CheckHelpers.Add(checks, "Runtime", "Inline Python script executes", pythonExe, smokeOk, smokeDetail);
        }

        var logPath = _artifactPathPolicy.CreateTimestampedLogFile(techRoot, "PythonSmoke", "txt");
        WriteLog(logPath, "INWC Python smoke check", techRoot, options.SkipRuntimeTest, checks);

        var hasFailure = CheckHelpers.HasFailure(checks);
        return new CommandResult
        {
            ExitCode = hasFailure ? _exitCodePolicy.Failure : _exitCodePolicy.Success,
            Message = hasFailure
                ? $"FAIL: Python smoke checks found {checks.Count(c => !c.Ok)} issue(s)."
                : "PASS: Python smoke checks passed.",
            Checks = checks,
            Artifacts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Log"] = logPath
            }
        };
    }

    private static void WriteLog(string logPath, string title, string techRoot, bool skipRuntimeTest, IReadOnlyList<CheckRecord> checks)
    {
        var lines = new List<string>
        {
            title,
            $"timestamp: {DateTime.Now:s}",
            $"tech_root: {techRoot}",
            $"skip_runtime_test: {skipRuntimeTest}",
            string.Empty,
            "Scope\tCheck\tTarget\tOK\tDetail"
        };

        lines.AddRange(checks.Select(c => $"{c.Scope}\t{c.Check}\t{c.Target}\t{c.Ok}\t{c.Detail}"));
        File.WriteAllLines(logPath, lines, Encoding.ASCII);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // No-op cleanup.
        }
    }
}

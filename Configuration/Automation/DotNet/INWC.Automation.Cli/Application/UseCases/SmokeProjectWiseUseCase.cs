using System.Text;
using System.Text.RegularExpressions;
using INWC.Automation.Cli.Application.Interfaces;
using INWC.Automation.Cli.Compatibility;
using INWC.Automation.Cli.Domain.Models;
using INWC.Automation.Cli.Infrastructure.Config;

namespace INWC.Automation.Cli.Application.UseCases;

internal sealed class SmokeProjectWiseUseCase : ICommandUseCase<SmokeProjectWiseOptions, CommandResult>
{
    private readonly IConfigFileReader _configReader;
    private readonly IArtifactPathPolicy _artifactPathPolicy;
    private readonly IExitCodePolicy _exitCodePolicy;

    public SmokeProjectWiseUseCase(
        IConfigFileReader configReader,
        IArtifactPathPolicy artifactPathPolicy,
        IExitCodePolicy exitCodePolicy)
    {
        _configReader = configReader;
        _artifactPathPolicy = artifactPathPolicy;
        _exitCodePolicy = exitCodePolicy;
    }

    public CommandResult Execute(CommandContext context, SmokeProjectWiseOptions options)
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
        var pwCfg = Path.Combine(cfgRoot, "Organization", "ByFunction", "DocumentManagement", "ProjectWiseDrive_Integration.cfg");

        foreach (var filePath in new[] { orgCfg, interopCfg, pwCfg })
        {
            CheckHelpers.Add(checks, "Config", "File exists", filePath, File.Exists(filePath));
        }

        var docVarRaw = _configReader.GetCfgVarValue(orgCfg, "FUNCTION_DOCUMENT_MANAGEMENT_CONFIG");
        var docVarResolved = _configReader.ResolveCfgTokens(docVarRaw, tokens);
        var docVarOk = !string.IsNullOrWhiteSpace(docVarResolved) && File.Exists(docVarResolved);
        CheckHelpers.Add(checks, "Config", "FUNCTION_DOCUMENT_MANAGEMENT_CONFIG resolves", docVarResolved ?? string.Empty, docVarOk, docVarRaw ?? string.Empty);

        var orgText = File.Exists(orgCfg) ? File.ReadAllText(orgCfg) : string.Empty;
        var hasDocInclude = Regex.IsMatch(orgText, "(?m)^\\s*%include\\s+\\$\\(FUNCTION_DOCUMENT_MANAGEMENT_CONFIG\\)\\s*$");
        CheckHelpers.Add(checks, "Config", "%include $(FUNCTION_DOCUMENT_MANAGEMENT_CONFIG) present", orgCfg, hasDocInclude);

        var interopRaw = _configReader.GetCfgVarValue(interopCfg, "INWC_INTEROP_PROJECTWISE_DRIVE");
        var toggleOk = string.Equals(interopRaw, "0", StringComparison.Ordinal) || string.Equals(interopRaw, "1", StringComparison.Ordinal);
        CheckHelpers.Add(checks, "Toggle", "INWC_INTEROP_PROJECTWISE_DRIVE is 0/1", interopCfg, toggleOk, interopRaw ?? string.Empty);

        var pwEnabledRaw = _configReader.GetCfgVarValue(pwCfg, "PROJECTWISE_DRIVE_ENABLED");
        var pwEnabledUsesToggle = string.Equals(pwEnabledRaw, "$(INWC_INTEROP_PROJECTWISE_DRIVE)", StringComparison.Ordinal);
        CheckHelpers.Add(checks, "Config", "PROJECTWISE_DRIVE_ENABLED uses interoperability toggle", pwCfg, pwEnabledUsesToggle, pwEnabledRaw ?? string.Empty);

        var pwExeRaw = _configReader.GetCfgVarValue(pwCfg, "PROJECTWISE_DRIVE_EXE");
        var pwExeResolved = _configReader.ResolveCfgTokens(pwExeRaw, tokens);
        var pwExeOk = !string.IsNullOrWhiteSpace(pwExeResolved) && File.Exists(pwExeResolved);
        CheckHelpers.Add(checks, "Executable", "PROJECTWISE_DRIVE_EXE exists", pwExeResolved ?? string.Empty, pwExeOk, pwExeRaw ?? string.Empty);

        var dirVars = new[]
        {
            "PROJECTWISE_DRIVE_WORK_DIR",
            "PROJECTWISE_DRIVE_SYNC_DIR",
            "PROJECTWISE_DRIVE_CACHE_DIR"
        };

        var resolvedDirs = new List<string>();
        foreach (var variable in dirVars)
        {
            var raw = _configReader.GetCfgVarValue(pwCfg, variable);
            var resolved = _configReader.ResolveCfgTokens(raw, tokens);
            var exists = !string.IsNullOrWhiteSpace(resolved) && Directory.Exists(resolved);
            CheckHelpers.Add(checks, "Folders", variable + " exists", resolved ?? string.Empty, exists, raw ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(resolved))
            {
                resolvedDirs.Add(resolved);
            }
        }

        if (options.SkipWriteTest)
        {
            foreach (var dir in resolvedDirs)
            {
                CheckHelpers.Add(checks, "WriteTest", "Skipped write test", dir, true, "SkipWriteTest switch provided");
            }
        }
        else
        {
            foreach (var dir in resolvedDirs)
            {
                if (!Directory.Exists(dir))
                {
                    CheckHelpers.Add(checks, "WriteTest", "Directory writable", dir, false, "Directory missing");
                    continue;
                }

                var tempFile = Path.Combine(dir, "._inwc_pw_smoke_" + Guid.NewGuid().ToString("N") + ".tmp");
                try
                {
                    File.WriteAllText(tempFile, "INWC ProjectWise smoke write test", Encoding.ASCII);
                    var wrote = File.Exists(tempFile);
                    if (wrote)
                    {
                        File.Delete(tempFile);
                    }

                    CheckHelpers.Add(checks, "WriteTest", "Directory writable", dir, wrote);
                }
                catch (Exception ex)
                {
                    CheckHelpers.Add(checks, "WriteTest", "Directory writable", dir, false, ex.Message);
                    TryDelete(tempFile);
                }
            }
        }

        var isRunning = System.Diagnostics.Process.GetProcessesByName("ProjectWise Drive").Length > 0;
        CheckHelpers.Add(checks, "Runtime", "ProjectWise Drive process running (informational)", "ProjectWise Drive", true, $"Running={isRunning}");

        var logPath = _artifactPathPolicy.CreateTimestampedLogFile(techRoot, "ProjectWiseSmoke", "txt");
        WriteLog(logPath, techRoot, options.SkipWriteTest, checks);

        var hasFailure = CheckHelpers.HasFailure(checks);
        return new CommandResult
        {
            ExitCode = hasFailure ? _exitCodePolicy.Failure : _exitCodePolicy.Success,
            Message = hasFailure
                ? $"FAIL: ProjectWise smoke checks found {checks.Count(c => !c.Ok)} issue(s)."
                : "PASS: ProjectWise smoke checks passed.",
            Checks = checks,
            Artifacts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Log"] = logPath
            }
        };
    }

    private static void WriteLog(string logPath, string techRoot, bool skipWriteTest, IReadOnlyList<CheckRecord> checks)
    {
        var lines = new List<string>
        {
            "INWC ProjectWise smoke check",
            $"timestamp: {DateTime.Now:s}",
            $"tech_root: {techRoot}",
            $"skip_write_test: {skipWriteTest}",
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
            // best effort
        }
    }
}

using System.Text;
using System.Text.RegularExpressions;
using INWC.Automation.Cli.Application.Interfaces;
using INWC.Automation.Cli.Catalog;
using INWC.Automation.Cli.Compatibility;
using INWC.Automation.Cli.Domain.Models;
using INWC.Automation.Cli.Infrastructure.Config;

namespace INWC.Automation.Cli.Application.UseCases;

internal sealed class DetectIntegrationsUseCase : ICommandUseCase<DetectIntegrationsOptions, CommandResult>
{
    private readonly IArtifactPathPolicy _artifactPathPolicy;
    private readonly IExitCodePolicy _exitCodePolicy;
    private readonly IConfigFileReader _configReader;

    public DetectIntegrationsUseCase(
        IArtifactPathPolicy artifactPathPolicy,
        IExitCodePolicy exitCodePolicy,
        IConfigFileReader configReader)
    {
        _artifactPathPolicy = artifactPathPolicy;
        _exitCodePolicy = exitCodePolicy;
        _configReader = configReader;
    }

    public CommandResult Execute(CommandContext context, DetectIntegrationsOptions options)
    {
        var interopCfg = Path.Combine(context.Global.TechRoot, "Configuration", "Organization", "Interoperability", "Interoperability.cfg");
        if (!File.Exists(interopCfg))
        {
            return CommandResult.Failure($"Interoperability config not found: {interopCfg}", _exitCodePolicy.Failure);
        }

        var cfgText = File.ReadAllText(interopCfg);
        var checks = new List<CheckRecord>();
        var changedCount = 0;

        foreach (var rule in IntegrationDetectionCatalog.Rules)
        {
            var existing = _configReader.GetCfgVarValue(interopCfg, rule.Variable);
            var foundCount = rule.Paths.Count(File.Exists);
            var requiredCount = rule.Paths.Length;
            var allFound = foundCount == requiredCount;
            var target = rule.ForceValue ?? (allFound ? "1" : "0");
            var changed = !string.Equals(existing, target, StringComparison.Ordinal);

            if (changed)
            {
                changedCount++;
            }

            if (options.Apply && changed && !context.Global.WhatIf)
            {
                cfgText = SetCfgVariable(cfgText, rule.Variable, target);
            }

            var detail = (rule.ForceValue is null ? "Detected" : "Forced") + $"; Current={existing}; Target={target}; Found={foundCount}/{requiredCount}";
            CheckHelpers.Add(checks, "IntegrationDetect", rule.Variable, interopCfg, true, detail);
        }

        if (options.Apply && !context.Global.WhatIf)
        {
            File.WriteAllText(interopCfg, cfgText, Encoding.ASCII);
        }

        var logPath = _artifactPathPolicy.CreateTimestampedLogFile(context.Global.TechRoot, "IntegrationDetect", "txt");
        WriteLog(logPath, context.Global.TechRoot, interopCfg, options.Apply, context.Global.WhatIf, checks, changedCount);

        return new CommandResult
        {
            ExitCode = _exitCodePolicy.Success,
            Message = context.Global.WhatIf
                ? $"Run mode: apply (what-if); changes detected: {changedCount}."
                : options.Apply
                    ? $"Run mode: apply; changes detected: {changedCount}."
                    : $"Run mode: dry-run; changes detected: {changedCount}.",
            Checks = checks,
            Artifacts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Log"] = logPath
            },
            Data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["ChangesDetected"] = changedCount,
                ["Mode"] = options.Apply ? "apply" : "dry-run"
            }
        };
    }

    private static string SetCfgVariable(string text, string variable, string value)
    {
        var replacement = $"{variable.PadRight(30)} = {value}";
        var pattern = $"(?m)^\\s*{Regex.Escape(variable)}\\s*=.*$";
        var regex = new Regex(pattern);

        if (regex.IsMatch(text))
        {
            return regex.Replace(text, replacement, 1);
        }

        var trimmed = text.TrimEnd('\r', '\n');
        if (string.IsNullOrEmpty(trimmed))
        {
            return replacement + "\r\n";
        }

        return trimmed + "\r\n" + replacement + "\r\n";
    }

    private static void WriteLog(
        string logPath,
        string techRoot,
        string interopCfg,
        bool apply,
        bool whatIf,
        IReadOnlyList<CheckRecord> checks,
        int changedCount)
    {
        var lines = new List<string>
        {
            "INWC integration detection",
            $"timestamp: {DateTime.Now:s}",
            $"mode: {(apply ? "apply" : "dry-run")}",
            $"what_if: {whatIf}",
            $"tech_root: {techRoot}",
            $"interop_cfg: {interopCfg}",
            string.Empty,
            "Scope\tCheck\tTarget\tOK\tDetail"
        };

        lines.AddRange(checks.Select(c => $"{c.Scope}\t{c.Check}\t{c.Target}\t{c.Ok}\t{c.Detail}"));
        lines.Add(string.Empty);
        lines.Add($"changes_detected: {changedCount}");
        lines.Add($"changes_applied: {(apply && !whatIf ? changedCount : 0)}");
        lines.Add(string.Empty);
        lines.Add("ProjectWise Drive follows detection and is enabled when path checks pass.");

        File.WriteAllLines(logPath, lines, Encoding.ASCII);
    }
}

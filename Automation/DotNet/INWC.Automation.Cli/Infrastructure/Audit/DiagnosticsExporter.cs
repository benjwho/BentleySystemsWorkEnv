using System.Text;
using System.Text.Json;
using INWC.Automation.Cli.Domain.Models;
using INWC.Automation.Cli.Infrastructure.Config;
using INWC.Automation.Cli.Infrastructure.Processes;
using INWC.Automation.Cli.Infrastructure.System;

namespace INWC.Automation.Cli.Infrastructure.Audit;

internal interface IDiagnosticsExporter
{
    IReadOnlyDictionary<string, string> Export(string techRoot, string auditRoot, IReadOnlyList<string> productConfigRoots);
}

internal sealed class DiagnosticsExporter : IDiagnosticsExporter
{
    private readonly IAuditLogger _logger;
    private readonly IConfigFileReader _configReader;
    private readonly IProcessRunner _processRunner;

    public DiagnosticsExporter(IAuditLogger logger, IConfigFileReader configReader, IProcessRunner processRunner)
    {
        _logger = logger;
        _configReader = configReader;
        _processRunner = processRunner;
    }

    public IReadOnlyDictionary<string, string> Export(string techRoot, string auditRoot, IReadOnlyList<string> productConfigRoots)
    {
        var artifacts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var envSnapshot = ExportEnvCheckSnapshot(techRoot, auditRoot);
        if (!string.IsNullOrWhiteSpace(envSnapshot))
        {
            artifacts["EnvCheck"] = envSnapshot;
        }

        var diagnosticsRoot = ExportConfigResolutionDiagnostics(techRoot, auditRoot, productConfigRoots);
        artifacts["DiagnosticsRoot"] = diagnosticsRoot;
        artifacts["ConfigResolutionSummary"] = Path.Combine(diagnosticsRoot, "ConfigResolutionSummary.txt");
        artifacts["ConfigResolutionCsv"] = Path.Combine(diagnosticsRoot, "ConfigResolutionSummary.csv");
        artifacts["ConfigResolutionJson"] = Path.Combine(diagnosticsRoot, "ConfigResolutionSummary.json");

        return artifacts;
    }

    private string? ExportEnvCheckSnapshot(string techRoot, string auditRoot)
    {
        var envCheckScript = Path.Combine(techRoot, "Configuration", "Automation", "PowerShell", "INWC.EnvCheck.ps1");
        if (!File.Exists(envCheckScript))
        {
            _logger.Warn("INWC.EnvCheck.ps1 missing; skipping env snapshot.");
            return null;
        }

        var result = _processRunner.Run(
            "powershell.exe",
            ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", envCheckScript, "-TechRoot", techRoot],
            techRoot);

        var snapshotPath = Path.Combine(auditRoot, "EnvCheck.txt");
        File.WriteAllText(snapshotPath, result.StdOut + result.StdErr, Encoding.ASCII);
        _logger.Info($"EnvCheck snapshot written: {snapshotPath} (ExitCode={result.ExitCode})");
        return snapshotPath;
    }

    private string ExportConfigResolutionDiagnostics(string techRoot, string auditRoot, IReadOnlyList<string> productConfigRoots)
    {
        var diagnosticsRoot = Path.Combine(auditRoot, "Diagnostics");
        Directory.CreateDirectory(diagnosticsRoot);

        var expectedCfgRoot = _configReader.NormalizeCfgRoot(Path.Combine(techRoot, "Configuration"));
        var userEnv = _configReader.NormalizeCfgRoot(Environment.GetEnvironmentVariable("_USTN_CUSTOM_CONFIGURATION", EnvironmentVariableTarget.User));
        var machineEnv = _configReader.NormalizeCfgRoot(Environment.GetEnvironmentVariable("_USTN_CUSTOM_CONFIGURATION", EnvironmentVariableTarget.Machine));
        var fallbackEnv = !string.IsNullOrWhiteSpace(userEnv) ? userEnv : machineEnv;

        var rows = new List<ConfigResolutionRow>();

        foreach (var productRoot in productConfigRoots)
        {
            var setupCfg = Path.Combine(productRoot, "ConfigurationSetup.cfg");
            if (!File.Exists(setupCfg))
            {
                rows.Add(new ConfigResolutionRow(productRoot, setupCfg, false, null, null, fallbackEnv, null, false));
                continue;
            }

            var safeName = FileNameUtil.SanitizeFileName(productRoot) + "_ConfigurationSetup.cfg";
            File.Copy(setupCfg, Path.Combine(diagnosticsRoot, safeName), true);

            var rawCustom = _configReader.GetCfgVarValue(setupCfg, "_USTN_CUSTOM_CONFIGURATION");
            var normalizedCustom = _configReader.NormalizeCfgRoot(rawCustom);
            var effectiveCustom = !string.IsNullOrWhiteSpace(normalizedCustom) ? normalizedCustom : fallbackEnv;
            var includePath = !string.IsNullOrWhiteSpace(effectiveCustom)
                ? effectiveCustom!.Replace('/', '\\') + "WorkSpaceSetup.cfg"
                : null;

            var includeResolves = !string.IsNullOrWhiteSpace(includePath) && File.Exists(includePath);
            rows.Add(new ConfigResolutionRow(productRoot, setupCfg, true, rawCustom, normalizedCustom, effectiveCustom, includePath, includeResolves));
        }

        var txtPath = Path.Combine(diagnosticsRoot, "ConfigResolutionSummary.txt");
        var csvPath = Path.Combine(diagnosticsRoot, "ConfigResolutionSummary.csv");
        var jsonPath = Path.Combine(diagnosticsRoot, "ConfigResolutionSummary.json");

        var sb = new StringBuilder();
        sb.AppendLine($"Timestamp: {DateTime.Now:s}");
        sb.AppendLine($"TechRoot: {techRoot}");
        sb.AppendLine($"Expected INWC cfg root: {expectedCfgRoot}");
        sb.AppendLine($"User _USTN_CUSTOM_CONFIGURATION: {userEnv}");
        sb.AppendLine($"Machine _USTN_CUSTOM_CONFIGURATION: {machineEnv}");
        sb.AppendLine();
        sb.AppendLine("Per-product _USTN_CUSTOM_CONFIGURATION resolution:");
        sb.AppendLine();

        foreach (var row in rows)
        {
            sb.AppendLine($"ProductRoot: {row.ProductRoot}");
            sb.AppendLine($"  ConfigSetup: {row.ConfigSetup}");
            sb.AppendLine($"  Exists: {row.Exists}");
            sb.AppendLine($"  RawCustom: {row.RawCustom}");
            sb.AppendLine($"  NormalizedCustom: {row.NormalizedCustom}");
            sb.AppendLine($"  EffectiveCustom: {row.EffectiveCustom}");
            sb.AppendLine($"  IncludePath: {row.IncludePath}");
            sb.AppendLine($"  IncludeResolves: {row.IncludeResolves}");
            sb.AppendLine();
        }

        File.WriteAllText(txtPath, sb.ToString(), Encoding.ASCII);
        File.WriteAllText(csvPath, BuildCsv(rows), Encoding.ASCII);
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }), Encoding.ASCII);

        _logger.Info($"Diagnostics summary written: {txtPath}");
        _logger.Info($"Diagnostics CSV written: {csvPath}");
        _logger.Info($"Diagnostics JSON written: {jsonPath}");

        return diagnosticsRoot;
    }

    private static string BuildCsv(IEnumerable<ConfigResolutionRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ProductRoot,ConfigSetup,Exists,RawCustom,NormalizedCustom,EffectiveCustom,IncludePath,IncludeResolves");

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                Csv(row.ProductRoot),
                Csv(row.ConfigSetup),
                Csv(row.Exists.ToString()),
                Csv(row.RawCustom),
                Csv(row.NormalizedCustom),
                Csv(row.EffectiveCustom),
                Csv(row.IncludePath),
                Csv(row.IncludeResolves.ToString())
            }));
        }

        return sb.ToString();
    }

    private static string Csv(string? value)
    {
        var text = value ?? string.Empty;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }
}

using INWC.Automation.Cli.Application.Interfaces;
using INWC.Automation.Cli.Catalog;
using INWC.Automation.Cli.Compatibility;
using INWC.Automation.Cli.Domain.Models;
using INWC.Automation.Cli.Infrastructure.Config;
using INWC.Automation.Cli.Infrastructure.System;

namespace INWC.Automation.Cli.Infrastructure.Audit;

internal sealed class PickerAuditService : IPickerAuditService
{
    private readonly IArtifactPathPolicy _artifactPathPolicy;
    private readonly IExecutableResolver _executableResolver;
    private readonly IConfigFileReader _configFileReader;

    public PickerAuditService(
        IArtifactPathPolicy artifactPathPolicy,
        IExecutableResolver executableResolver,
        IConfigFileReader configFileReader)
    {
        _artifactPathPolicy = artifactPathPolicy;
        _executableResolver = executableResolver;
        _configFileReader = configFileReader;
    }

    public CommandResult RunAudit(CommandContext context, AuditPickerOptions options)
    {
        var auditRoot = _artifactPathPolicy.CreateTimestampedRunFolder(context.Global.TechRoot, "PickerAuditCs");
        var logPath = Path.Combine(auditRoot, "PickerAuditCs.log");

        var logger = new AuditLogger(logPath);
        var readinessMonitor = new ReadinessSignalMonitor(logger);
        var windowManager = new WindowManager();
        var screenshotService = new ScreenshotService();
        var appSessionRunner = new AppSessionRunner(logger, windowManager, screenshotService, readinessMonitor);
        var processRunner = new Processes.ProcessRunner();
        var diagnosticsExporter = new DiagnosticsExporter(logger, _configFileReader, processRunner);

        logger.Info("INWC C# picker audit starting.");
        logger.Info($"TechRoot: {context.Global.TechRoot}");
        logger.Info($"AuditRoot: {auditRoot}");
        logger.Info($"SkipLaunch={options.SkipLaunch}; KeepAppsOpen={options.KeepAppsOpen}; WaitSeconds={options.WaitSeconds}; UserActionSeconds={options.UserActionSeconds}; WaitForEnterBeforeCapture={options.WaitForEnterBeforeCapture}; AutoPilot={options.AutoPilot}; ReadinessSettleSeconds={options.ReadinessSettleSeconds}");

        var sourceAutoPilotPath = options.AutoPilotFilePath;
        if (string.IsNullOrWhiteSpace(sourceAutoPilotPath))
        {
            sourceAutoPilotPath = Path.Combine(context.Global.TechRoot, "Configuration", "Standards", "Seed", "NWC_Rehab_Seed.dgn");
        }

        var runAutoPilotPath = PrepareRunAutoPilotFilePath(options.AutoPilot, sourceAutoPilotPath, auditRoot);
        if (options.AutoPilot)
        {
            logger.Info($"AutoPilotFilePath(source)={sourceAutoPilotPath}");
            logger.Info($"AutoPilotFilePath(run)={runAutoPilotPath}");
        }

        var screenshots = new List<string>();
        if (!options.SkipLaunch)
        {
            foreach (var app in AppCatalog.Apps)
            {
                try
                {
                    var executablePath = _executableResolver.Resolve(app);
                    var screenshot = appSessionRunner.Run(options, app, executablePath, runAutoPilotPath, auditRoot);
                    screenshots.Add(screenshot);
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                }
            }
        }
        else
        {
            logger.Info("SkipLaunch enabled: no apps were started.");
        }

        var needDiagnostics = options.PickerFailed;
        if (!options.NonInteractive && !options.PickerFailed)
        {
            Console.Write("If picker failed or looked wrong, type Y to export diagnostics now: ");
            var answer = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(answer)
                && answer.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase))
            {
                needDiagnostics = true;
            }
        }

        var artifacts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AuditRoot"] = auditRoot,
            ["AuditLog"] = logPath
        };

        if (needDiagnostics)
        {
            logger.Info("Collecting diagnostics because picker failure was indicated.");
            foreach (var kvp in diagnosticsExporter.Export(context.Global.TechRoot, auditRoot, ProductConfigCatalog.DefaultRoots))
            {
                artifacts[kvp.Key] = kvp.Value;
            }
        }
        else
        {
            logger.Info("Diagnostics skipped. Re-run with --picker-failed to force diagnostics export.");
        }

        logger.Info("Picker audit complete.");
        logger.Info($"Log: {logPath}");

        for (var i = 0; i < screenshots.Count; i++)
        {
            artifacts[$"Screenshot{i + 1}"] = screenshots[i];
            logger.Info($"Screenshot: {screenshots[i]}");
        }

        return new CommandResult
        {
            ExitCode = 0,
            Message = "Picker audit complete.",
            Artifacts = artifacts
        };
    }

    private static string? PrepareRunAutoPilotFilePath(bool autoPilotEnabled, string? sourcePath, string auditRoot)
    {
        if (!autoPilotEnabled || string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return sourcePath;
        }

        try
        {
            var scratchRoot = Path.Combine(auditRoot, "Scratch");
            Directory.CreateDirectory(scratchRoot);

            var extension = Path.GetExtension(sourcePath);
            var fileName = Path.GetFileNameWithoutExtension(sourcePath);
            var runtimeCopy = Path.Combine(scratchRoot, fileName + "_runtime" + extension);
            File.Copy(sourcePath, runtimeCopy, true);
            return runtimeCopy;
        }
        catch
        {
            return sourcePath;
        }
    }
}

using System.Diagnostics;
using INWC.Automation.Cli.Domain.Models;
using INWC.Automation.Cli.Infrastructure.System;

namespace INWC.Automation.Cli.Infrastructure.Audit;

internal interface IAppSessionRunner
{
    string Run(AuditPickerOptions options, AppDefinition app, string executablePath, string? runAutoPilotFilePath, string auditRoot);
}

internal sealed class AppSessionRunner : IAppSessionRunner
{
    private readonly IAuditLogger _logger;
    private readonly IWindowManager _windowManager;
    private readonly IScreenshotService _screenshotService;
    private readonly IReadinessSignalMonitor _readinessSignalMonitor;

    public AppSessionRunner(
        IAuditLogger logger,
        IWindowManager windowManager,
        IScreenshotService screenshotService,
        IReadinessSignalMonitor readinessSignalMonitor)
    {
        _logger = logger;
        _windowManager = windowManager;
        _screenshotService = screenshotService;
        _readinessSignalMonitor = readinessSignalMonitor;
    }

    public string Run(AuditPickerOptions options, AppDefinition app, string executablePath, string? runAutoPilotFilePath, string auditRoot)
    {
        _logger.Info($"Launching {app.Name}: {executablePath}");

        var psi = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = true
        };

        if (options.AutoPilot && !string.IsNullOrWhiteSpace(runAutoPilotFilePath) && File.Exists(runAutoPilotFilePath))
        {
            psi.Arguments = QuoteArg(runAutoPilotFilePath);
            _logger.Info($"AutoPilot launch-arg mode for {app.Name}: {runAutoPilotFilePath}");
        }

        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start {app.Name}");
        _logger.Info($"Started {app.Name} PID={process.Id}");

        var launchAt = DateTime.UtcNow;
        if (_windowManager.Focus(process, 20))
        {
            _logger.Info($"Focused window for {app.Name} before screenshot.");
        }
        else
        {
            _logger.Warn($"Could not focus {app.Name} window before screenshot.");
        }

        var readinessSignal = _readinessSignalMonitor.WaitForSignal(process, app, launchAt, options.WaitSeconds);
        if (readinessSignal is not null)
        {
            _logger.Info($"Readiness signal for {app.Name}: source={readinessSignal.Source}; file={readinessSignal.FilePath}; utc={readinessSignal.TimestampUtc:O}");
            if (options.ReadinessSettleSeconds > 0)
            {
                _logger.Info($"Readiness settle wait: {options.ReadinessSettleSeconds}s for {app.Name}.");
                Thread.Sleep(TimeSpan.FromSeconds(options.ReadinessSettleSeconds));
            }
        }
        else
        {
            _logger.Warn($"No software signal found within {options.WaitSeconds}s for {app.Name}; capturing on timeout fallback.");
        }

        if (!options.AutoPilot)
        {
            if (options.WaitForEnterBeforeCapture && !options.NonInteractive)
            {
                Console.Write($"Set workspace/workset and open a file in {app.Name}, then press Enter to capture screenshot: ");
                Console.ReadLine();
            }
            else if (options.UserActionSeconds > 0)
            {
                _logger.Info($"Manual prep window: {options.UserActionSeconds}s for {app.Name} (set workspace/workset and open file).");
                Thread.Sleep(TimeSpan.FromSeconds(options.UserActionSeconds));
            }
        }

        var shotPath = Path.Combine(auditRoot, $"{FileNameUtil.SanitizeFileName(app.Name)}_Picker_{DateTime.Now:HHmmss}.png");
        if (TryCaptureTargetWindow(process, shotPath))
        {
            _logger.Info($"Screenshot saved (window-scoped): {shotPath}");
        }
        else
        {
            _logger.Warn($"Window-scoped capture failed for {app.Name}; falling back to full desktop capture.");
            _screenshotService.CaptureDesktop(shotPath);
            _logger.Info($"Screenshot saved (desktop fallback): {shotPath}");
        }

        if (!options.KeepAppsOpen)
        {
            _windowManager.Close(process, _logger.Warn);
            _logger.Info($"Closed {app.Name} (if still running).");
        }
        else
        {
            _logger.Info($"Left {app.Name} running due to --keep-apps-open.");
        }

        return shotPath;
    }

    private bool TryCaptureTargetWindow(Process process, string outputPath)
    {
        const int maxAttempts = 8;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            _windowManager.Focus(process, 2);
            var handle = _windowManager.GetMainWindowHandle(process);
            var ownsForeground = _windowManager.IsForegroundWindowOwnedBy(process);

            if (handle != IntPtr.Zero && ownsForeground && _screenshotService.CaptureWindow(handle, outputPath))
            {
                return true;
            }

            Thread.Sleep(350);
        }

        return false;
    }

    private static string QuoteArg(string value)
    {
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }
}

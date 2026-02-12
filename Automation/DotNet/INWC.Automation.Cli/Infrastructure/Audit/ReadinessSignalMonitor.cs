using System.Diagnostics;
using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Infrastructure.Audit;

internal interface IReadinessSignalMonitor
{
    SoftwareSignal? WaitForSignal(Process process, AppDefinition app, DateTime launchAtUtc, int waitSeconds);
}

internal sealed class ReadinessSignalMonitor : IReadinessSignalMonitor
{
    private readonly IAuditLogger _logger;

    public ReadinessSignalMonitor(IAuditLogger logger)
    {
        _logger = logger;
    }

    public SoftwareSignal? WaitForSignal(Process process, AppDefinition app, DateTime launchAtUtc, int waitSeconds)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var featureTrackingRoot = Path.Combine(localAppData, "Bentley", "FeatureTracking");
        var appPrefsRoot = Path.Combine(localAppData, "Bentley", app.LocalProductToken, "25.0.0", "prefs");
        var genericLogsRoot = Path.Combine(localAppData, "Bentley", "Logs");

        var timeoutSeconds = Math.Max(1, waitSeconds);
        var minSignalUtc = launchAtUtc.AddSeconds(Math.Min(8, timeoutSeconds));
        var deadline = launchAtUtc.AddSeconds(timeoutSeconds);
        SoftwareSignal? latest = null;

        _logger.Info($"Waiting for software data/log signal for {app.Name} (timeout={timeoutSeconds}s).");

        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                _logger.Warn($"{app.Name} exited before readiness signal.");
                return latest;
            }

            foreach (var signal in CollectSignals(process.Id, app, featureTrackingRoot, appPrefsRoot, genericLogsRoot, launchAtUtc))
            {
                if (latest is null || signal.TimestampUtc > latest.TimestampUtc)
                {
                    latest = signal;
                }
            }

            var titleReady = HasReadyMainWindowTitle(process);
            if (latest is not null && latest.TimestampUtc >= minSignalUtc && titleReady)
            {
                return latest;
            }

            Thread.Sleep(500);
        }

        return latest;
    }

    private static IEnumerable<SoftwareSignal> CollectSignals(
        int pid,
        AppDefinition app,
        string featureTrackingRoot,
        string appPrefsRoot,
        string genericLogsRoot,
        DateTime launchAtUtc)
    {
        if (Directory.Exists(featureTrackingRoot))
        {
            foreach (var file in SafeEnumerateFiles(featureTrackingRoot, $"FeatureLog.{pid}*.db*", SearchOption.TopDirectoryOnly))
            {
                var info = SafeFileInfo(file);
                if (info is not null && info.Length > 0 && info.LastWriteTimeUtc >= launchAtUtc)
                {
                    yield return new SoftwareSignal("FeatureTrackingDb", file, info.LastWriteTimeUtc);
                }
            }
        }

        if (Directory.Exists(appPrefsRoot))
        {
            var prefCandidates = new[] { "Personal.upf", "startscreen.xml", "cache.ucf" };
            foreach (var name in prefCandidates)
            {
                var path = Path.Combine(appPrefsRoot, name);
                var info = SafeFileInfo(path);
                if (info is not null && info.LastWriteTimeUtc >= launchAtUtc)
                {
                    yield return new SoftwareSignal("AppPrefs", path, info.LastWriteTimeUtc);
                }
            }
        }

        if (Directory.Exists(genericLogsRoot))
        {
            foreach (var file in SafeEnumerateFiles(genericLogsRoot, "*.log", SearchOption.AllDirectories))
            {
                if (!file.Contains(app.LocalProductToken, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var info = SafeFileInfo(file);
                if (info is not null && info.LastWriteTimeUtc >= launchAtUtc)
                {
                    yield return new SoftwareSignal("BentleyLog", file, info.LastWriteTimeUtc);
                }
            }
        }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string root, string pattern, SearchOption searchOption)
    {
        try
        {
            return Directory.EnumerateFiles(root, pattern, searchOption).ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static FileInfo? SafeFileInfo(string path)
    {
        try
        {
            return File.Exists(path) ? new FileInfo(path) : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool HasReadyMainWindowTitle(Process process)
    {
        process.Refresh();
        if (process.MainWindowHandle == IntPtr.Zero)
        {
            return false;
        }

        var title = process.MainWindowTitle;
        return !string.IsNullOrWhiteSpace(title)
               && !title.Contains("splash", StringComparison.OrdinalIgnoreCase);
    }
}

using System.Diagnostics;
using Microsoft.VisualBasic;
using INWC.Automation.Cli.Catalog;
using INWC.Automation.Cli.Domain.Runtime;

namespace INWC.Automation.Cli.Infrastructure.Runtime;

internal interface IRuntimeEventWatcher
{
    IReadOnlyList<RuntimeEvent> Poll(DateTime nowUtc);
}

internal sealed class ProcessStartWatcher : IRuntimeEventWatcher
{
    private readonly Dictionary<int, string> _knownProcessAppByPid = [];

    public IReadOnlyList<RuntimeEvent> Poll(DateTime nowUtc)
    {
        var events = new List<RuntimeEvent>();
        var livePids = new HashSet<int>();

        foreach (var app in AppCatalog.Apps)
        {
            foreach (var pattern in app.Patterns)
            {
                var processName = Path.GetFileNameWithoutExtension(pattern);
                Process[] processes;
                try
                {
                    processes = Process.GetProcessesByName(processName);
                }
                catch
                {
                    continue;
                }

                foreach (var process in processes)
                {
                    livePids.Add(process.Id);
                    if (_knownProcessAppByPid.ContainsKey(process.Id))
                    {
                        continue;
                    }

                    _knownProcessAppByPid[process.Id] = app.Name;
                    events.Add(new RuntimeEvent
                    {
                        Name = "app.started",
                        Source = "process-watcher",
                        TimestampUtc = nowUtc,
                        AppName = app.Name,
                        ProcessId = process.Id,
                        Attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["process_name"] = process.ProcessName
                        }
                    });
                }
            }
        }

        var stale = _knownProcessAppByPid.Keys.Where(pid => !livePids.Contains(pid)).ToArray();
        foreach (var pid in stale)
        {
            _knownProcessAppByPid.Remove(pid);
        }

        return events;
    }
}

internal sealed class DgnOpenWatcher : IRuntimeEventWatcher
{
    private string? _lastDesignFileKey;

    public IReadOnlyList<RuntimeEvent> Poll(DateTime nowUtc)
    {
        var events = new List<RuntimeEvent>();
        if (!IsCivilPlatformRunning())
        {
            _lastDesignFileKey = null;
            return events;
        }

        var active = TryGetActiveDesignInfo();
        if (active is null)
        {
            return events;
        }

        var fileKey = (active.Value.Path ?? active.Value.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(fileKey))
        {
            return events;
        }

        if (string.Equals(fileKey, _lastDesignFileKey, StringComparison.OrdinalIgnoreCase))
        {
            return events;
        }

        _lastDesignFileKey = fileKey;
        events.Add(new RuntimeEvent
        {
            Name = "dgn.opened",
            Source = "dgn-watcher",
            TimestampUtc = nowUtc,
            AppName = active.Value.AppName,
            ProcessId = active.Value.ProcessId,
            DesignFilePath = active.Value.Path ?? active.Value.Name,
            Attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["design_name"] = active.Value.Name ?? string.Empty,
                ["design_path"] = active.Value.Path ?? string.Empty
            }
        });

        return events;
    }

    private static bool IsCivilPlatformRunning()
    {
        foreach (var app in AppCatalog.Apps)
        {
            foreach (var pattern in app.Patterns)
            {
                var processName = Path.GetFileNameWithoutExtension(pattern);
                try
                {
                    if (Process.GetProcessesByName(processName).Length > 0)
                    {
                        return true;
                    }
                }
                catch
                {
                    // ignored on purpose
                }
            }
        }

        return false;
    }

    private static (string? Name, string? Path, string? AppName, int? ProcessId)? TryGetActiveDesignInfo()
    {
        try
        {
            dynamic? app = Interaction.GetObject(string.Empty, "MicroStation.Application");
            if (app is null)
            {
                return null;
            }

            string? fileName = null;
            string? filePath = null;
            int? processId = null;

            try
            {
                if (app.ActiveDesignFile != null)
                {
                    fileName = app.ActiveDesignFile.Name;
                    filePath = app.ActiveDesignFile.Path;
                }
            }
            catch
            {
                // ignored on purpose
            }

            try
            {
                processId = app.ProcessID;
            }
            catch
            {
                // ignored on purpose
            }

            string? caption = null;
            try
            {
                caption = app.Caption;
            }
            catch
            {
                // ignored on purpose
            }

            return (fileName, filePath, caption, processId);
        }
        catch
        {
            return null;
        }
    }
}

internal sealed class PeriodicHealthSignalWatcher : IRuntimeEventWatcher
{
    private readonly RuntimePathProvider _pathProvider;
    private DateTime _lastSeenSignalUtc;

    public PeriodicHealthSignalWatcher(RuntimePathProvider pathProvider)
    {
        _pathProvider = pathProvider;
        _pathProvider.EnsureServiceStateDirectory();
    }

    public IReadOnlyList<RuntimeEvent> Poll(DateTime nowUtc)
    {
        var signalPath = Path.Combine(_pathProvider.ServiceStateRoot, "periodic-health.signal");
        if (!File.Exists(signalPath))
        {
            return [];
        }

        DateTime signalUtc;
        try
        {
            signalUtc = File.GetLastWriteTimeUtc(signalPath);
        }
        catch
        {
            return [];
        }

        if (signalUtc <= _lastSeenSignalUtc)
        {
            return [];
        }

        _lastSeenSignalUtc = signalUtc;
        return
        [
            new RuntimeEvent
            {
                Name = "periodic.health",
                Source = "service-signal",
                TimestampUtc = nowUtc
            }
        ];
    }
}

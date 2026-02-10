namespace INWC.Automation.Cli.Infrastructure.Runtime;

internal sealed class RuntimePathProvider
{
    public RuntimePathProvider(string techRoot)
    {
        TechRoot = techRoot;
    }

    public string TechRoot { get; }
    public string RuntimeRoot => Path.Combine(TechRoot, "Logs", "NWC_Rehab", "RuntimeBridge");
    public string QueueRoot => Path.Combine(RuntimeRoot, "queue");
    public string QueuePendingRoot => Path.Combine(QueueRoot, "pending");
    public string QueueApprovedRoot => Path.Combine(QueueRoot, "approved");
    public string QueueInProgressRoot => Path.Combine(QueueRoot, "inprogress");
    public string QueueRejectedRoot => Path.Combine(QueueRoot, "rejected");
    public string RunsRoot => Path.Combine(RuntimeRoot, "runs");
    public string EventsRoot => Path.Combine(RuntimeRoot, "events");
    public string StateRoot => Path.Combine(RuntimeRoot, "state");
    public string RulesDefaultPath => Path.Combine(TechRoot, "Configuration", "Automation", "Runtime", "runtime-rules.json");
    public string ServiceStateRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "INWC", "RuntimeBridge", "service-state");

    public void EnsureRuntimeDirectories()
    {
        Directory.CreateDirectory(RuntimeRoot);
        Directory.CreateDirectory(QueueRoot);
        Directory.CreateDirectory(QueuePendingRoot);
        Directory.CreateDirectory(QueueApprovedRoot);
        Directory.CreateDirectory(QueueInProgressRoot);
        Directory.CreateDirectory(QueueRejectedRoot);
        Directory.CreateDirectory(RunsRoot);
        Directory.CreateDirectory(EventsRoot);
        Directory.CreateDirectory(StateRoot);
    }

    public void EnsureServiceStateDirectory()
    {
        Directory.CreateDirectory(ServiceStateRoot);
    }

    public string ResolveRulesPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return RulesDefaultPath;
        }

        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        return Path.GetFullPath(Path.Combine(TechRoot, path));
    }
}

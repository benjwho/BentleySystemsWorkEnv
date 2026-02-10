using System.Text;
using System.Text.Json;
using INWC.Automation.Cli.Domain.Models;
using INWC.Automation.Cli.Domain.Runtime;
using INWC.Automation.Cli.Infrastructure.Runtime.Rules;

namespace INWC.Automation.Cli.Infrastructure.Runtime;

internal sealed class RuntimeServiceCoordinator
{
    private readonly RuntimePathProvider _pathProvider;
    private readonly IRuntimeRulesLoader _rulesLoader;
    private readonly RuntimeEventPlanner _planner;
    private readonly RuntimeEventLogger _eventLogger;

    public RuntimeServiceCoordinator(
        RuntimePathProvider pathProvider,
        IRuntimeRulesLoader rulesLoader,
        RuntimeEventPlanner planner,
        RuntimeEventLogger eventLogger)
    {
        _pathProvider = pathProvider;
        _rulesLoader = rulesLoader;
        _planner = planner;
        _eventLogger = eventLogger;
    }

    public void Run(CommandContext context, string? rulesPath, CancellationToken cancellationToken)
    {
        _pathProvider.EnsureServiceStateDirectory();
        var resolvedRulesPath = _pathProvider.ResolveRulesPath(rulesPath);
        var rules = _rulesLoader.Load(resolvedRulesPath);

        var pollSeconds = Math.Max(1, rules.Defaults.PollSeconds);
        var periodicSeconds = Math.Max(30, rules.Defaults.PeriodicHealthSeconds);
        var lastPeriodicUtc = DateTime.MinValue;

        _eventLogger.LogMessage("service_start", "Runtime service coordinator started.", new { resolvedRulesPath, pollSeconds, periodicSeconds });

        while (!cancellationToken.IsCancellationRequested)
        {
            WriteHeartbeat();

            var nowUtc = DateTime.UtcNow;
            if ((nowUtc - lastPeriodicUtc).TotalSeconds >= periodicSeconds)
            {
                var runtimeEvent = new RuntimeEvent
                {
                    Name = "periodic.health",
                    Source = "runtime-service",
                    TimestampUtc = nowUtc
                };
                _planner.Plan(context, rules, runtimeEvent);
                TouchPeriodicSignal();
                lastPeriodicUtc = nowUtc;
            }

            Thread.Sleep(pollSeconds * 1000);
        }

        _eventLogger.LogMessage("service_stop", "Runtime service coordinator stopped.");
    }

    private void WriteHeartbeat()
    {
        var heartbeatPath = Path.Combine(_pathProvider.ServiceStateRoot, "heartbeat.json");
        var payload = new
        {
            timestampUtc = DateTime.UtcNow.ToString("O"),
            machine = Environment.MachineName,
            processId = Environment.ProcessId
        };

        var json = JsonSerializer.Serialize(payload);
        File.WriteAllText(heartbeatPath, json, Encoding.ASCII);
    }

    private void TouchPeriodicSignal()
    {
        var signalPath = Path.Combine(_pathProvider.ServiceStateRoot, "periodic-health.signal");
        if (!File.Exists(signalPath))
        {
            File.WriteAllText(signalPath, DateTime.UtcNow.ToString("O"), Encoding.ASCII);
        }
        else
        {
            File.SetLastWriteTimeUtc(signalPath, DateTime.UtcNow);
        }
    }
}

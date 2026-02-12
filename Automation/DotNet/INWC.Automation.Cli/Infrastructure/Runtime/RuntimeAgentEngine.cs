using INWC.Automation.Cli.Domain.Models;
using INWC.Automation.Cli.Domain.Runtime;
using INWC.Automation.Cli.Infrastructure.Runtime.Execution;
using INWC.Automation.Cli.Infrastructure.Runtime.Rules;

namespace INWC.Automation.Cli.Infrastructure.Runtime;

internal sealed class RuntimeAgentEngine
{
    private readonly RuntimePathProvider _pathProvider;
    private readonly IRuntimeRulesLoader _rulesLoader;
    private readonly RuntimeEventPlanner _planner;
    private readonly RuntimeQueueStore _queueStore;
    private readonly RuntimeActionExecutor _actionExecutor;
    private readonly RuntimeEventLogger _eventLogger;

    public RuntimeAgentEngine(
        RuntimePathProvider pathProvider,
        IRuntimeRulesLoader rulesLoader,
        RuntimeEventPlanner planner,
        RuntimeQueueStore queueStore,
        RuntimeActionExecutor actionExecutor,
        RuntimeEventLogger eventLogger)
    {
        _pathProvider = pathProvider;
        _rulesLoader = rulesLoader;
        _planner = planner;
        _queueStore = queueStore;
        _actionExecutor = actionExecutor;
        _eventLogger = eventLogger;
    }

    public CommandResult Start(CommandContext context, RuntimeAgentStartOptions options)
    {
        _pathProvider.EnsureRuntimeDirectories();
        var rulesPath = _pathProvider.ResolveRulesPath(options.RulesPath);
        var rules = _rulesLoader.Load(rulesPath);
        var pollSeconds = options.PollSeconds > 0 ? options.PollSeconds : rules.Defaults.PollSeconds;

        var watchers = new List<IRuntimeEventWatcher>
        {
            new ProcessStartWatcher(),
            new DgnOpenWatcher(),
            new PeriodicHealthSignalWatcher(_pathProvider)
        };

        var cts = new CancellationTokenSource();
        if (!options.Once && Environment.UserInteractive)
        {
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
            };
        }

        var stats = RunLoop(context, rules, watchers, pollSeconds, options.Once, cts.Token);
        var artifacts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["RuntimeRoot"] = _pathProvider.RuntimeRoot,
            ["RulesPath"] = rulesPath
        };

        return new CommandResult
        {
            ExitCode = 0,
            Message = options.Once
                ? "Runtime agent executed one polling cycle."
                : "Runtime agent stopped.",
            Artifacts = artifacts,
            Data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["queued_count"] = stats.QueuedCount,
                ["executed_count"] = stats.ExecutedCount,
                ["failed_count"] = stats.FailedCount
            }
        };
    }

    private LoopStats RunLoop(
        CommandContext context,
        RuntimeRulesDocument rules,
        IReadOnlyList<IRuntimeEventWatcher> watchers,
        int pollSeconds,
        bool once,
        CancellationToken cancellationToken)
    {
        var periodicFallback = DateTime.UtcNow;
        var stats = new LoopStats();

        while (!cancellationToken.IsCancellationRequested)
        {
            var nowUtc = DateTime.UtcNow;
            var events = new List<RuntimeEvent>();
            foreach (var watcher in watchers)
            {
                events.AddRange(watcher.Poll(nowUtc));
            }

            var periodicEvery = Math.Max(30, rules.Defaults.PeriodicHealthSeconds);
            if ((nowUtc - periodicFallback).TotalSeconds >= periodicEvery)
            {
                events.Add(new RuntimeEvent
                {
                    Name = "periodic.health",
                    Source = "agent-local-fallback",
                    TimestampUtc = nowUtc
                });
                periodicFallback = nowUtc;
            }

            foreach (var runtimeEvent in events)
            {
                var plan = _planner.Plan(context, rules, runtimeEvent);
                stats.QueuedCount += plan.Intents.Count;
            }

            if (_queueStore.TryDequeueApproved(out var entry) && entry is not null)
            {
                var startedUtc = DateTime.UtcNow;
                var execution = _actionExecutor.Execute(context, entry.Intent);
                var finishedUtc = DateTime.UtcNow;

                var runRecord = new RuntimeRunRecord
                {
                    IntentId = entry.Intent.Id,
                    ActionId = entry.Intent.ActionId,
                    StartedUtc = startedUtc,
                    FinishedUtc = finishedUtc,
                    ExitCode = execution.ExitCode,
                    Message = execution.Message,
                    StdOut = execution.StdOut,
                    StdErr = execution.StdErr,
                    Artifacts = execution.Artifacts,
                    Data = execution.Data
                };

                if (execution.ExitCode != 0
                    && !entry.Intent.RequiresApproval
                    && entry.Intent.RetryCount < entry.Intent.MaxRetries)
                {
                    var retryIntent = new RuntimeActionIntent
                    {
                        Id = entry.Intent.Id + "-retry-" + (entry.Intent.RetryCount + 1),
                        ActionId = entry.Intent.ActionId,
                        ActionType = entry.Intent.ActionType,
                        CliArgs = new List<string>(entry.Intent.CliArgs),
                        ScriptPath = entry.Intent.ScriptPath,
                        ScriptArgs = new List<string>(entry.Intent.ScriptArgs),
                        RequiresApproval = false,
                        RetryCount = entry.Intent.RetryCount + 1,
                        MaxRetries = entry.Intent.MaxRetries,
                        CreatedUtc = DateTime.UtcNow,
                        TriggerEvent = entry.Intent.TriggerEvent,
                        Note = "automatic retry"
                    };

                    Thread.Sleep(Math.Max(1, rules.Defaults.RetryBackoffSeconds) * 1000);
                    _queueStore.Enqueue(retryIntent, RuntimeQueueState.Approved);
                    _eventLogger.LogMessage(
                        "retry_enqueued",
                        $"Requeued action '{entry.Intent.ActionId}' as '{retryIntent.Id}' after failure.",
                        new { original_intent_id = entry.Intent.Id, retry_intent_id = retryIntent.Id, execution.ExitCode });
                }

                _queueStore.Complete(entry, runRecord);
                stats.ExecutedCount++;
                if (execution.ExitCode != 0)
                {
                    stats.FailedCount++;
                }
            }

            if (once)
            {
                break;
            }

            Thread.Sleep(Math.Max(1, pollSeconds) * 1000);
        }

        return stats;
    }

    private sealed class LoopStats
    {
        public int QueuedCount { get; set; }
        public int ExecutedCount { get; set; }
        public int FailedCount { get; set; }
    }
}

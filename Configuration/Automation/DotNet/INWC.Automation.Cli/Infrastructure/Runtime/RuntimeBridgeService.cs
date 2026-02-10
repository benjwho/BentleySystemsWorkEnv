using System.ServiceProcess;
using System.Text.Json;
using INWC.Automation.Cli.Application.Interfaces;
using INWC.Automation.Cli.Domain.Models;
using INWC.Automation.Cli.Domain.Runtime;
using INWC.Automation.Cli.Infrastructure.Config;
using INWC.Automation.Cli.Infrastructure.Processes;
using INWC.Automation.Cli.Infrastructure.Runtime.Execution;
using INWC.Automation.Cli.Infrastructure.Runtime.Rules;

namespace INWC.Automation.Cli.Infrastructure.Runtime;

internal sealed class RuntimeBridgeService : IRuntimeBridgeService
{
    private readonly IConfigFileReader _configReader;
    private readonly IProcessRunner _processRunner;

    public RuntimeBridgeService(IConfigFileReader configReader, IProcessRunner processRunner)
    {
        _configReader = configReader;
        _processRunner = processRunner;
    }

    public CommandResult RuntimeAgentStart(CommandContext context, RuntimeAgentStartOptions options)
    {
        var runtime = BuildRuntimeDependencies(context.Global.TechRoot);
        return runtime.AgentEngine.Start(context, options);
    }

    public CommandResult RuntimeAgentInstall(CommandContext context, RuntimeAgentInstallOptions options)
    {
        var runtime = BuildRuntimeDependencies(context.Global.TechRoot);
        var result = runtime.Management.InstallAgentTask(runtime.PathProvider.RulesDefaultPath);
        return ManagementResultToCommandResult(result);
    }

    public CommandResult RuntimeAgentUninstall(CommandContext context, RuntimeAgentUninstallOptions options)
    {
        var runtime = BuildRuntimeDependencies(context.Global.TechRoot);
        var result = runtime.Management.UninstallAgentTask();
        return ManagementResultToCommandResult(result);
    }

    public CommandResult RuntimeServiceRun(CommandContext context, RuntimeServiceRunOptions options)
    {
        var runtime = BuildRuntimeDependencies(context.Global.TechRoot);
        var resolvedRulesPath = runtime.PathProvider.ResolveRulesPath(options.RulesPath);

        if (!Environment.UserInteractive)
        {
            ServiceBase.Run(new RuntimeWindowsServiceHost(runtime.ServiceCoordinator, context, resolvedRulesPath));
            return CommandResult.Success("Runtime service stopped.");
        }

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
        };

        runtime.ServiceCoordinator.Run(context, resolvedRulesPath, cts.Token);
        return CommandResult.Success("Runtime service run loop stopped.");
    }

    public CommandResult RuntimeServiceInstall(CommandContext context, RuntimeServiceInstallOptions options)
    {
        var runtime = BuildRuntimeDependencies(context.Global.TechRoot);
        var result = runtime.Management.InstallService(runtime.PathProvider.RulesDefaultPath);
        return ManagementResultToCommandResult(result);
    }

    public CommandResult RuntimeServiceStart(CommandContext context, RuntimeServiceStartOptions options)
    {
        var runtime = BuildRuntimeDependencies(context.Global.TechRoot);
        var result = runtime.Management.StartService();
        return ManagementResultToCommandResult(result);
    }

    public CommandResult RuntimeServiceStop(CommandContext context, RuntimeServiceStopOptions options)
    {
        var runtime = BuildRuntimeDependencies(context.Global.TechRoot);
        var result = runtime.Management.StopService();
        return ManagementResultToCommandResult(result);
    }

    public CommandResult RuntimeServiceUninstall(CommandContext context, RuntimeServiceUninstallOptions options)
    {
        var runtime = BuildRuntimeDependencies(context.Global.TechRoot);
        var result = runtime.Management.UninstallService();
        return ManagementResultToCommandResult(result);
    }

    public CommandResult RuntimeServiceStatus(CommandContext context, RuntimeServiceStatusOptions options)
    {
        var runtime = BuildRuntimeDependencies(context.Global.TechRoot);
        var result = runtime.Management.QueryService();
        return ManagementResultToCommandResult(result);
    }

    public CommandResult RuntimeQueueList(CommandContext context, RuntimeQueueListOptions options)
    {
        var runtime = BuildRuntimeDependencies(context.Global.TechRoot);
        var entries = runtime.QueueStore.ListAll();
        var grouped = entries.GroupBy(e => e.State).ToDictionary(g => g.Key.ToString(), g => g.Count());
        var resultData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["counts"] = grouped,
            ["items"] = entries.Select(entry => new
            {
                state = entry.State.ToString(),
                id = entry.Intent.Id,
                actionId = entry.Intent.ActionId,
                actionType = entry.Intent.ActionType,
                createdUtc = entry.Intent.CreatedUtc,
                requiresApproval = entry.Intent.RequiresApproval,
                eventName = entry.Intent.TriggerEvent.Name,
                appName = entry.Intent.TriggerEvent.AppName
            }).ToArray()
        };

        return new CommandResult
        {
            ExitCode = 0,
            Message = $"Runtime queue contains {entries.Count} item(s).",
            Data = resultData,
            Artifacts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["QueueRoot"] = runtime.PathProvider.QueueRoot
            }
        };
    }

    public CommandResult RuntimeApprove(CommandContext context, RuntimeApproveOptions options)
    {
        var runtime = BuildRuntimeDependencies(context.Global.TechRoot);
        var ok = runtime.QueueStore.TryApprove(options.ActionId, options.Note, out var intent);
        if (!ok || intent is null)
        {
            return CommandResult.Failure($"Pending action not found: {options.ActionId}", 1);
        }

        return new CommandResult
        {
            ExitCode = 0,
            Message = $"Approved runtime action: {options.ActionId}",
            Data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["action_id"] = options.ActionId,
                ["note"] = options.Note
            }
        };
    }

    public CommandResult RuntimeReject(CommandContext context, RuntimeRejectOptions options)
    {
        var runtime = BuildRuntimeDependencies(context.Global.TechRoot);
        var ok = runtime.QueueStore.TryReject(options.ActionId, options.Note, out var intent);
        if (!ok || intent is null)
        {
            return CommandResult.Failure($"Pending action not found: {options.ActionId}", 1);
        }

        return new CommandResult
        {
            ExitCode = 0,
            Message = $"Rejected runtime action: {options.ActionId}",
            Data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["action_id"] = options.ActionId,
                ["note"] = options.Note
            }
        };
    }

    public CommandResult RuntimeTrigger(CommandContext context, RuntimeTriggerOptions options)
    {
        var runtime = BuildRuntimeDependencies(context.Global.TechRoot);
        var rulesPath = runtime.PathProvider.RulesDefaultPath;
        var rules = runtime.RulesLoader.Load(rulesPath);

        var payload = LoadPayload(options.PayloadJsonPath, context.Global.TechRoot);
        var evt = new RuntimeEvent
        {
            Name = options.EventName,
            Source = "runtime-trigger-cli",
            TimestampUtc = DateTime.UtcNow,
            Attributes = payload
        };

        var planningResult = runtime.Planner.Plan(context, rules, evt);
        return new CommandResult
        {
            ExitCode = 0,
            Message = $"Queued {planningResult.Intents.Count} action(s) for event '{options.EventName}'.",
            Data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["event_name"] = options.EventName,
                ["queued"] = planningResult.Intents.Select(i => new { i.Id, i.ActionId, i.ActionType, i.RequiresApproval }).ToArray()
            }
        };
    }

    private RuntimeDependencies BuildRuntimeDependencies(string techRoot)
    {
        var pathProvider = new RuntimePathProvider(techRoot);
        var rulesLoader = new RuntimeRulesLoader();
        var queueStore = new RuntimeQueueStore(pathProvider);
        var stateStore = new RuntimeStateStore(pathProvider);
        var eventLogger = new RuntimeEventLogger(pathProvider);
        var planner = new RuntimeEventPlanner(queueStore, stateStore, eventLogger);
        var actionExecutor = new RuntimeActionExecutor(pathProvider, _configReader, _processRunner);
        var agentEngine = new RuntimeAgentEngine(pathProvider, rulesLoader, planner, queueStore, actionExecutor, eventLogger);
        var serviceCoordinator = new RuntimeServiceCoordinator(pathProvider, rulesLoader, planner, eventLogger);
        var management = new RuntimeManagementService(pathProvider, _processRunner);

        return new RuntimeDependencies(pathProvider, rulesLoader, queueStore, planner, agentEngine, serviceCoordinator, management);
    }

    private static IReadOnlyDictionary<string, string> LoadPayload(string? payloadPath, string techRoot)
    {
        if (string.IsNullOrWhiteSpace(payloadPath))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var resolved = Path.IsPathRooted(payloadPath)
            ? Path.GetFullPath(payloadPath)
            : Path.GetFullPath(Path.Combine(techRoot, payloadPath));

        if (!File.Exists(resolved))
        {
            throw new FileNotFoundException("Payload JSON path not found.", resolved);
        }

        var raw = File.ReadAllText(resolved);
        using var doc = JsonDocument.Parse(raw);
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            dict[prop.Name] = prop.Value.ToString();
        }

        return dict;
    }

    private static CommandResult ManagementResultToCommandResult(RuntimeManagementResult result)
    {
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["stdout"] = result.StdOut,
            ["stderr"] = result.StdErr
        };

        return new CommandResult
        {
            ExitCode = result.ExitCode,
            Message = result.Message,
            Data = data
        };
    }

    private sealed record RuntimeDependencies(
        RuntimePathProvider PathProvider,
        IRuntimeRulesLoader RulesLoader,
        RuntimeQueueStore QueueStore,
        RuntimeEventPlanner Planner,
        RuntimeAgentEngine AgentEngine,
        RuntimeServiceCoordinator ServiceCoordinator,
        RuntimeManagementService Management);
}

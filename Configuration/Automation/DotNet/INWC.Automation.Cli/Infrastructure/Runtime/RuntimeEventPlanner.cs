using INWC.Automation.Cli.Domain.Models;
using INWC.Automation.Cli.Domain.Runtime;
using INWC.Automation.Cli.Infrastructure.Runtime.Execution;

namespace INWC.Automation.Cli.Infrastructure.Runtime;

internal sealed class RuntimeEventPlanner
{
    private readonly RuntimeQueueStore _queueStore;
    private readonly RuntimeStateStore _stateStore;
    private readonly RuntimeEventLogger _eventLogger;

    public RuntimeEventPlanner(RuntimeQueueStore queueStore, RuntimeStateStore stateStore, RuntimeEventLogger eventLogger)
    {
        _queueStore = queueStore;
        _stateStore = stateStore;
        _eventLogger = eventLogger;
    }

    public RuntimeEventPlanningResult Plan(CommandContext context, RuntimeRulesDocument rules, RuntimeEvent runtimeEvent)
    {
        var nowUtc = runtimeEvent.TimestampUtc;
        var accepted = _stateStore.ShouldAcceptEvent(runtimeEvent.Fingerprint(), rules.Defaults.DedupeWindowSeconds, nowUtc);
        if (!accepted)
        {
            _eventLogger.LogMessage("event_deduped", $"Skipped duplicate event '{runtimeEvent.Name}'.", runtimeEvent);
            return new RuntimeEventPlanningResult(runtimeEvent, []);
        }

        var enabledProfiles = rules.Profiles.ToDictionary(p => p.Id, p => p.Enabled, StringComparer.OrdinalIgnoreCase);
        var actionsById = rules.Actions.ToDictionary(a => a.Id, StringComparer.OrdinalIgnoreCase);

        var matchingTriggers = rules.Triggers
            .Where(t => t.Enabled
                        && t.Event.Equals(runtimeEvent.Name, StringComparison.OrdinalIgnoreCase)
                        && AppPatternMatches(t.AppPattern, runtimeEvent.AppName))
            .ToList();

        var intents = new List<RuntimeActionIntent>();
        foreach (var trigger in matchingTriggers)
        {
            foreach (var binding in rules.Bindings.Where(b =>
                         b.Enabled
                         && b.TriggerId.Equals(trigger.Id, StringComparison.OrdinalIgnoreCase)
                         && AppPatternMatches(b.AppPattern, runtimeEvent.AppName)))
            {
                foreach (var actionId in binding.ActionIds)
                {
                    if (!actionsById.TryGetValue(actionId, out var action))
                    {
                        continue;
                    }

                    if (!action.Enabled)
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(action.Profile)
                        && enabledProfiles.TryGetValue(action.Profile, out var profileEnabled)
                        && !profileEnabled)
                    {
                        continue;
                    }

                    var scope = BuildScopeKey(runtimeEvent);
                    var cooldown = action.CooldownSeconds > 0 ? action.CooldownSeconds : rules.Defaults.DedupeWindowSeconds;
                    if (!_stateStore.ShouldQueueAction(action.Id, scope, cooldown, nowUtc))
                    {
                        continue;
                    }

                    var requiresApproval = action.RequiresApproval || RuntimeMutationClassifier.IsMutating(action);
                    var intent = BuildIntent(rules, runtimeEvent, action, requiresApproval);
                    _queueStore.Enqueue(intent, requiresApproval ? RuntimeQueueState.Pending : RuntimeQueueState.Approved);
                    intents.Add(intent);
                }
            }
        }

        _eventLogger.LogEvent(runtimeEvent, intents);
        return new RuntimeEventPlanningResult(runtimeEvent, intents);
    }

    private static RuntimeActionIntent BuildIntent(RuntimeRulesDocument rules, RuntimeEvent runtimeEvent, RuntimeActionRule action, bool requiresApproval)
    {
        var maxRetries = action.MaxRetries >= 0 ? action.MaxRetries : rules.Defaults.MaxRetries;
        return new RuntimeActionIntent
        {
            Id = BuildIntentId(action.Id),
            ActionId = action.Id,
            ActionType = action.Type,
            CliArgs = new List<string>(action.CliArgs),
            ScriptPath = action.ScriptPath,
            ScriptArgs = new List<string>(action.ScriptArgs),
            CreatedUtc = DateTime.UtcNow,
            TriggerEvent = runtimeEvent,
            RequiresApproval = requiresApproval,
            MaxRetries = maxRetries < 0 ? 0 : maxRetries
        };
    }

    private static string BuildIntentId(string actionId)
    {
        var safeAction = actionId.Replace(' ', '-').Replace('_', '-');
        return $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{safeAction}-{Guid.NewGuid():N}".ToLowerInvariant();
    }

    private static bool AppPatternMatches(string? pattern, string? appName)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(appName))
        {
            return false;
        }

        return appName.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildScopeKey(RuntimeEvent runtimeEvent)
    {
        var app = runtimeEvent.AppName ?? string.Empty;
        var dgn = runtimeEvent.DesignFilePath ?? string.Empty;
        return $"{runtimeEvent.Name}|{app}|{dgn}";
    }
}

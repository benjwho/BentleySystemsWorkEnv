using System.Text.Json;
using INWC.Automation.Cli.Domain.Runtime;

namespace INWC.Automation.Cli.Infrastructure.Runtime.Rules;

internal interface IRuntimeRulesLoader
{
    RuntimeRulesDocument Load(string path);
}

internal sealed class RuntimeRulesLoader : IRuntimeRulesLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public RuntimeRulesDocument Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Runtime rules JSON not found.", path);
        }

        var json = File.ReadAllText(path);
        var rules = JsonSerializer.Deserialize<RuntimeRulesDocument>(json, JsonOptions)
                    ?? throw new InvalidOperationException("Failed to parse runtime rules JSON.");

        Validate(rules);
        return rules;
    }

    private static void Validate(RuntimeRulesDocument rules)
    {
        if (string.IsNullOrWhiteSpace(rules.Version))
        {
            throw new InvalidOperationException("runtime-rules.json: version is required.");
        }

        var profileIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in rules.Profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Id))
            {
                throw new InvalidOperationException("runtime-rules.json: profile id cannot be empty.");
            }

            if (!profileIds.Add(profile.Id))
            {
                throw new InvalidOperationException($"runtime-rules.json: duplicate profile id '{profile.Id}'.");
            }
        }

        var triggerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var trigger in rules.Triggers)
        {
            if (string.IsNullOrWhiteSpace(trigger.Id) || string.IsNullOrWhiteSpace(trigger.Event))
            {
                throw new InvalidOperationException("runtime-rules.json: trigger id/event cannot be empty.");
            }

            if (!triggerIds.Add(trigger.Id))
            {
                throw new InvalidOperationException($"runtime-rules.json: duplicate trigger id '{trigger.Id}'.");
            }
        }

        var actionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var action in rules.Actions)
        {
            if (string.IsNullOrWhiteSpace(action.Id))
            {
                throw new InvalidOperationException("runtime-rules.json: action id cannot be empty.");
            }

            if (!actionIds.Add(action.Id))
            {
                throw new InvalidOperationException($"runtime-rules.json: duplicate action id '{action.Id}'.");
            }

            if (!string.Equals(action.Type, "cli", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(action.Type, "script.python", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"runtime-rules.json: unsupported action type '{action.Type}' for action '{action.Id}'.");
            }

            if (string.Equals(action.Type, "cli", StringComparison.OrdinalIgnoreCase) && action.CliArgs.Count == 0)
            {
                throw new InvalidOperationException($"runtime-rules.json: action '{action.Id}' type=cli requires cliArgs.");
            }

            if (string.Equals(action.Type, "script.python", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(action.ScriptPath))
            {
                throw new InvalidOperationException($"runtime-rules.json: action '{action.Id}' type=script.python requires scriptPath.");
            }

            if (!string.IsNullOrWhiteSpace(action.Profile) && !profileIds.Contains(action.Profile))
            {
                throw new InvalidOperationException($"runtime-rules.json: action '{action.Id}' references unknown profile '{action.Profile}'.");
            }
        }

        foreach (var binding in rules.Bindings)
        {
            if (string.IsNullOrWhiteSpace(binding.TriggerId))
            {
                throw new InvalidOperationException("runtime-rules.json: binding triggerId cannot be empty.");
            }

            if (!triggerIds.Contains(binding.TriggerId))
            {
                throw new InvalidOperationException($"runtime-rules.json: binding references unknown trigger '{binding.TriggerId}'.");
            }

            foreach (var actionId in binding.ActionIds)
            {
                if (!actionIds.Contains(actionId))
                {
                    throw new InvalidOperationException($"runtime-rules.json: binding for trigger '{binding.TriggerId}' references unknown action '{actionId}'.");
                }
            }
        }

        if (rules.Defaults.PollSeconds < 1)
        {
            throw new InvalidOperationException("runtime-rules.json: defaults.pollSeconds must be >= 1.");
        }

        if (rules.Defaults.DedupeWindowSeconds < 1)
        {
            throw new InvalidOperationException("runtime-rules.json: defaults.dedupeWindowSeconds must be >= 1.");
        }
    }
}

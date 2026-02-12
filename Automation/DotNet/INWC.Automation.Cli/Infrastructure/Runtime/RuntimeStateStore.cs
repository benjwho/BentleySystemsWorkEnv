using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace INWC.Automation.Cli.Infrastructure.Runtime;

internal sealed class RuntimeStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _statePath;
    private readonly Mutex _mutex;

    public RuntimeStateStore(RuntimePathProvider pathProvider)
    {
        pathProvider.EnsureRuntimeDirectories();
        _statePath = Path.Combine(pathProvider.StateRoot, "runtime-state.json");
        _mutex = new Mutex(false, BuildMutexName(pathProvider.TechRoot));
    }

    public bool ShouldAcceptEvent(string fingerprint, int dedupeWindowSeconds, DateTime nowUtc)
    {
        return WithState(state =>
        {
            var key = NormalizeKey(fingerprint);
            if (state.EventLastSeenUtc.TryGetValue(key, out var raw)
                && DateTime.TryParse(raw, out var seen)
                && (nowUtc - seen).TotalSeconds < dedupeWindowSeconds)
            {
                return false;
            }

            state.EventLastSeenUtc[key] = nowUtc.ToString("O");
            return true;
        });
    }

    public bool ShouldQueueAction(string actionId, string scope, int cooldownSeconds, DateTime nowUtc)
    {
        if (cooldownSeconds <= 0)
        {
            return true;
        }

        return WithState(state =>
        {
            var key = NormalizeKey(actionId + "|" + scope);
            if (state.ActionLastQueuedUtc.TryGetValue(key, out var raw)
                && DateTime.TryParse(raw, out var seen)
                && (nowUtc - seen).TotalSeconds < cooldownSeconds)
            {
                return false;
            }

            state.ActionLastQueuedUtc[key] = nowUtc.ToString("O");
            return true;
        });
    }

    private T WithState<T>(Func<RuntimeStateDocument, T> action)
    {
        _mutex.WaitOne();
        try
        {
            var state = Load();
            var result = action(state);
            Save(state);
            return result;
        }
        finally
        {
            _mutex.ReleaseMutex();
        }
    }

    private RuntimeStateDocument Load()
    {
        if (!File.Exists(_statePath))
        {
            return new RuntimeStateDocument();
        }

        try
        {
            var json = File.ReadAllText(_statePath);
            return JsonSerializer.Deserialize<RuntimeStateDocument>(json) ?? new RuntimeStateDocument();
        }
        catch
        {
            return new RuntimeStateDocument();
        }
    }

    private void Save(RuntimeStateDocument state)
    {
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(_statePath, json, Encoding.ASCII);
    }

    private static string NormalizeKey(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static string BuildMutexName(string techRoot)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFullPath(techRoot).ToLowerInvariant()));
        var shortHash = Convert.ToHexString(hash).Substring(0, 16);
        return @"Global\INWC_RuntimeBridge_" + shortHash;
    }
}

internal sealed class RuntimeStateDocument
{
    public Dictionary<string, string> EventLastSeenUtc { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> ActionLastQueuedUtc { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

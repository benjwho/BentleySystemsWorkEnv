namespace INWC.Automation.Cli.Domain.Runtime;

internal sealed class RuntimeEvent
{
    public string Name { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public string? AppName { get; init; }
    public int? ProcessId { get; init; }
    public string? DesignFilePath { get; init; }
    public IReadOnlyDictionary<string, string> Attributes { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public string Fingerprint()
    {
        return string.Join(
            "|",
            Name.Trim().ToLowerInvariant(),
            (AppName ?? string.Empty).Trim().ToLowerInvariant(),
            (DesignFilePath ?? string.Empty).Trim().ToLowerInvariant());
    }
}

internal sealed class RuntimeEventPlanningResult
{
    public RuntimeEventPlanningResult(RuntimeEvent runtimeEvent, IReadOnlyList<RuntimeActionIntent> intents)
    {
        RuntimeEvent = runtimeEvent;
        Intents = intents;
    }

    public RuntimeEvent RuntimeEvent { get; }
    public IReadOnlyList<RuntimeActionIntent> Intents { get; }
}

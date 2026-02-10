namespace INWC.Automation.Cli.Domain.Runtime;

internal enum RuntimeQueueState
{
    Pending,
    Approved,
    InProgress,
    Rejected
}

internal sealed class RuntimeActionIntent
{
    public string Id { get; set; } = string.Empty;
    public string ActionId { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public List<string> CliArgs { get; set; } = [];
    public string? ScriptPath { get; set; }
    public List<string> ScriptArgs { get; set; } = [];
    public bool RequiresApproval { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public RuntimeEvent TriggerEvent { get; set; } = new();
    public string? Note { get; set; }
}

internal sealed record RuntimeQueueEntry(RuntimeQueueState State, string Path, RuntimeActionIntent Intent);

internal sealed class RuntimeActionExecutionResult
{
    public int ExitCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public string StdOut { get; init; } = string.Empty;
    public string StdErr { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> Artifacts { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, object?> Data { get; init; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
}

internal sealed class RuntimeRunRecord
{
    public string IntentId { get; set; } = string.Empty;
    public string ActionId { get; set; } = string.Empty;
    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
    public DateTime FinishedUtc { get; set; } = DateTime.UtcNow;
    public int ExitCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string StdOut { get; set; } = string.Empty;
    public string StdErr { get; set; } = string.Empty;
    public IReadOnlyDictionary<string, string> Artifacts { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, object?> Data { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
}

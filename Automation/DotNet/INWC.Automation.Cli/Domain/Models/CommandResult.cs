namespace INWC.Automation.Cli.Domain.Models;

internal sealed class CommandResult
{
    public int ExitCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<CheckRecord> Checks { get; init; } = Array.Empty<CheckRecord>();
    public IReadOnlyList<NamedStatus> Statuses { get; init; } = Array.Empty<NamedStatus>();
    public IReadOnlyDictionary<string, string> Artifacts { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, object?> Data { get; init; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    public static CommandResult Success(string message) => new() { ExitCode = 0, Message = message };
    public static CommandResult Failure(string message, int exitCode = 1) => new() { ExitCode = exitCode, Message = message };
}

internal sealed record NamedStatus(string Name, int ExitCode, bool Ok);

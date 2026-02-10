using System.Text;
using System.Text.Json;
using INWC.Automation.Cli.Domain.Runtime;

namespace INWC.Automation.Cli.Infrastructure.Runtime;

internal sealed class RuntimeEventLogger
{
    private readonly RuntimePathProvider _pathProvider;

    public RuntimeEventLogger(RuntimePathProvider pathProvider)
    {
        _pathProvider = pathProvider;
        _pathProvider.EnsureRuntimeDirectories();
    }

    public void LogEvent(RuntimeEvent runtimeEvent, IReadOnlyList<RuntimeActionIntent> intents)
    {
        var payload = new
        {
            timestampUtc = DateTime.UtcNow.ToString("O"),
            eventName = runtimeEvent.Name,
            runtimeEvent.Source,
            runtimeEvent.AppName,
            runtimeEvent.ProcessId,
            runtimeEvent.DesignFilePath,
            runtimeEvent.Attributes,
            queued = intents.Select(i => new { i.Id, i.ActionId, i.ActionType, i.RequiresApproval })
        };

        AppendJsonLine("events", payload);
    }

    public void LogMessage(string category, string message, object? extra = null)
    {
        var payload = new
        {
            timestampUtc = DateTime.UtcNow.ToString("O"),
            category,
            message,
            extra
        };

        AppendJsonLine("messages", payload);
    }

    private void AppendJsonLine(string prefix, object payload)
    {
        var filePath = Path.Combine(_pathProvider.EventsRoot, $"{prefix}_{DateTime.Now:yyyyMMdd}.jsonl");
        Directory.CreateDirectory(_pathProvider.EventsRoot);
        var line = JsonSerializer.Serialize(payload);
        File.AppendAllText(filePath, line + Environment.NewLine, Encoding.ASCII);
    }
}

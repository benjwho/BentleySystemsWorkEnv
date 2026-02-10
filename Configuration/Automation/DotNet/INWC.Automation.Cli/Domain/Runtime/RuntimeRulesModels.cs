namespace INWC.Automation.Cli.Domain.Runtime;

internal sealed class RuntimeRulesDocument
{
    public string Version { get; set; } = "1.0";
    public RuntimeDefaults Defaults { get; set; } = new();
    public List<RuntimeProfileRule> Profiles { get; set; } = [];
    public List<RuntimeTriggerRule> Triggers { get; set; } = [];
    public List<RuntimeActionRule> Actions { get; set; } = [];
    public List<RuntimeBindingRule> Bindings { get; set; } = [];
}

internal sealed class RuntimeDefaults
{
    public int PollSeconds { get; set; } = 10;
    public int DedupeWindowSeconds { get; set; } = 30;
    public int ActionTimeoutSeconds { get; set; } = 300;
    public int RetryBackoffSeconds { get; set; } = 5;
    public int MaxRetries { get; set; } = 1;
    public int PeriodicHealthSeconds { get; set; } = 900;
}

internal sealed class RuntimeProfileRule
{
    public string Id { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string? Description { get; set; }
}

internal sealed class RuntimeTriggerRule
{
    public string Id { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string? AppPattern { get; set; }
}

internal sealed class RuntimeActionRule
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Profile { get; set; }
    public bool Enabled { get; set; } = true;
    public bool RequiresApproval { get; set; }
    public int CooldownSeconds { get; set; } = 60;
    public int MaxRetries { get; set; } = -1;
    public List<string> CliArgs { get; set; } = [];
    public string? ScriptPath { get; set; }
    public List<string> ScriptArgs { get; set; } = [];
}

internal sealed class RuntimeBindingRule
{
    public string TriggerId { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string? AppPattern { get; set; }
    public List<string> ActionIds { get; set; } = [];
}

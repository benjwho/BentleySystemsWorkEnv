namespace INWC.Automation.Cli.Domain.Models;

internal interface ICommandOptions;

internal sealed record CheckEnvOptions(IReadOnlyList<string> ProductConfigRoots) : ICommandOptions;
internal sealed record CheckIntegrationOptions() : ICommandOptions;
internal sealed record CheckFullHealthOptions(bool SkipPythonRuntimeTest, bool SkipProjectWiseWriteTest) : ICommandOptions;
internal sealed record FixPickerRootsOptions(bool Apply) : ICommandOptions;

internal sealed record AuditPickerOptions(
    bool AutoPilot,
    string? AutoPilotFilePath,
    bool SkipLaunch,
    bool KeepAppsOpen,
    bool PickerFailed,
    int WaitSeconds,
    int ReadinessSettleSeconds,
    int UserActionSeconds,
    bool WaitForEnterBeforeCapture,
    bool NonInteractive) : ICommandOptions;

internal sealed record SmokePythonOptions(bool SkipRuntimeTest) : ICommandOptions;
internal sealed record SmokeProjectWiseOptions(bool SkipWriteTest) : ICommandOptions;
internal sealed record DetectIntegrationsOptions(bool Apply) : ICommandOptions;
internal sealed record RepairProgramDataOptions(IReadOnlyList<string> ProductConfigRoots) : ICommandOptions;
internal sealed record BackupCreateOptions(bool IncludeUserPrefs, IReadOnlyList<string> ProductConfigRoots) : ICommandOptions;
internal sealed record BackupRestoreOptions(string FromPath) : ICommandOptions;
internal sealed record ResetRebuildOptions(
    bool ResetUserPrefs,
    bool RestoreOldestCfgBackup,
    bool IncludeLogsInBackup,
    IReadOnlyList<string> ProductConfigRoots) : ICommandOptions;
internal sealed record InitWorkspaceReportOptions() : ICommandOptions;
internal sealed record RuntimeAgentStartOptions(string? RulesPath, int PollSeconds, bool Once) : ICommandOptions;
internal sealed record RuntimeAgentInstallOptions() : ICommandOptions;
internal sealed record RuntimeAgentUninstallOptions() : ICommandOptions;
internal sealed record RuntimeServiceRunOptions(string? RulesPath) : ICommandOptions;
internal sealed record RuntimeServiceInstallOptions() : ICommandOptions;
internal sealed record RuntimeServiceStartOptions() : ICommandOptions;
internal sealed record RuntimeServiceStopOptions() : ICommandOptions;
internal sealed record RuntimeServiceUninstallOptions() : ICommandOptions;
internal sealed record RuntimeServiceStatusOptions() : ICommandOptions;
internal sealed record RuntimeQueueListOptions() : ICommandOptions;
internal sealed record RuntimeApproveOptions(string ActionId, string? Note) : ICommandOptions;
internal sealed record RuntimeRejectOptions(string ActionId, string? Note) : ICommandOptions;
internal sealed record RuntimeTriggerOptions(string EventName, string? PayloadJsonPath) : ICommandOptions;

internal sealed record CliInvocation(CommandId CommandId, GlobalOptions Global, ICommandOptions Options);

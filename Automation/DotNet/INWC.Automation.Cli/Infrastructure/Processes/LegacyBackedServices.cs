using INWC.Automation.Cli.Application.Interfaces;
using INWC.Automation.Cli.Compatibility;
using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Infrastructure.Processes;

internal sealed class LegacyRepairService : IRepairService
{
    private readonly ILegacyScriptBridge _legacy;

    public LegacyRepairService(ILegacyScriptBridge legacy)
    {
        _legacy = legacy;
    }

    public CommandResult RepairProgramData(CommandContext context, RepairProgramDataOptions options)
    {
        var args = new List<string> { "-TechRoot", context.Global.TechRoot };
        if (options.ProductConfigRoots.Count > 0)
        {
            args.Add("-ProductConfigRoots");
            args.Add(LegacyArgumentFormatter.ToArrayLiteral(options.ProductConfigRoots));
        }

        if (context.Global.WhatIf)
        {
            args.Add("-WhatIf");
        }

        return _legacy.Invoke(context, "INWC.Repair.ps1", args);
    }
}

internal sealed class LegacyBackupRestoreService : IBackupRestoreService
{
    private readonly ILegacyScriptBridge _legacy;

    public LegacyBackupRestoreService(ILegacyScriptBridge legacy)
    {
        _legacy = legacy;
    }

    public CommandResult BackupCreate(CommandContext context, BackupCreateOptions options)
    {
        var args = new List<string> { "-TechRoot", context.Global.TechRoot };
        if (options.ProductConfigRoots.Count > 0)
        {
            args.Add("-ProductConfigRoots");
            args.Add(LegacyArgumentFormatter.ToArrayLiteral(options.ProductConfigRoots));
        }

        if (options.IncludeUserPrefs)
        {
            args.Add("-IncludeUserPrefs");
        }

        if (context.Global.WhatIf)
        {
            args.Add("-WhatIf");
        }

        return _legacy.Invoke(context, "INWC.BackupRestore.ps1", args);
    }

    public CommandResult BackupRestore(CommandContext context, BackupRestoreOptions options)
    {
        var args = new List<string>
        {
            "-TechRoot", context.Global.TechRoot,
            "-RestoreFrom", options.FromPath
        };

        if (context.Global.WhatIf)
        {
            args.Add("-WhatIf");
        }

        return _legacy.Invoke(context, "INWC.BackupRestore.ps1", args);
    }
}

internal sealed class LegacyResetRebuildService : IResetRebuildService
{
    private readonly ILegacyScriptBridge _legacy;

    public LegacyResetRebuildService(ILegacyScriptBridge legacy)
    {
        _legacy = legacy;
    }

    public CommandResult ResetRebuild(CommandContext context, ResetRebuildOptions options)
    {
        var args = new List<string> { "-TechRoot", context.Global.TechRoot };
        if (options.ProductConfigRoots.Count > 0)
        {
            args.Add("-ProductConfigRoots");
            args.Add(LegacyArgumentFormatter.ToArrayLiteral(options.ProductConfigRoots));
        }

        if (options.ResetUserPrefs)
        {
            args.Add("-ResetUserPrefs");
        }

        if (options.RestoreOldestCfgBackup)
        {
            args.Add("-RestoreOldestCfgBackup");
        }

        if (options.IncludeLogsInBackup)
        {
            args.Add("-IncludeLogsInBackup");
        }

        if (context.Global.WhatIf)
        {
            args.Add("-WhatIf");
        }

        return _legacy.Invoke(context, "INWC.ResetRebuild.ps1", args);
    }
}

internal static class LegacyArgumentFormatter
{
    public static string ToArrayLiteral(IReadOnlyList<string> values)
    {
        var escaped = values.Select(value => "'" + value.Replace("'", "''") + "'");
        return "@(" + string.Join(",", escaped) + ")";
    }
}

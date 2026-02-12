using INWC.Automation.Cli.Application.Interfaces;
using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Application.UseCases;

internal sealed class RepairProgramDataUseCase : ICommandUseCase<RepairProgramDataOptions, CommandResult>
{
    private readonly IRepairService _repairService;

    public RepairProgramDataUseCase(IRepairService repairService)
    {
        _repairService = repairService;
    }

    public CommandResult Execute(CommandContext context, RepairProgramDataOptions options)
    {
        return _repairService.RepairProgramData(context, options);
    }
}

internal sealed class BackupCreateUseCase : ICommandUseCase<BackupCreateOptions, CommandResult>
{
    private readonly IBackupRestoreService _backupRestoreService;

    public BackupCreateUseCase(IBackupRestoreService backupRestoreService)
    {
        _backupRestoreService = backupRestoreService;
    }

    public CommandResult Execute(CommandContext context, BackupCreateOptions options)
    {
        return _backupRestoreService.BackupCreate(context, options);
    }
}

internal sealed class BackupRestoreUseCase : ICommandUseCase<BackupRestoreOptions, CommandResult>
{
    private readonly IBackupRestoreService _backupRestoreService;

    public BackupRestoreUseCase(IBackupRestoreService backupRestoreService)
    {
        _backupRestoreService = backupRestoreService;
    }

    public CommandResult Execute(CommandContext context, BackupRestoreOptions options)
    {
        return _backupRestoreService.BackupRestore(context, options);
    }
}

internal sealed class ResetRebuildUseCase : ICommandUseCase<ResetRebuildOptions, CommandResult>
{
    private readonly IResetRebuildService _resetRebuildService;

    public ResetRebuildUseCase(IResetRebuildService resetRebuildService)
    {
        _resetRebuildService = resetRebuildService;
    }

    public CommandResult Execute(CommandContext context, ResetRebuildOptions options)
    {
        return _resetRebuildService.ResetRebuild(context, options);
    }
}

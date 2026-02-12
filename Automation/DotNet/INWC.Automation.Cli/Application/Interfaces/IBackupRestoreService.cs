using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Application.Interfaces;

internal interface IBackupRestoreService
{
    CommandResult BackupCreate(CommandContext context, BackupCreateOptions options);
    CommandResult BackupRestore(CommandContext context, BackupRestoreOptions options);
}

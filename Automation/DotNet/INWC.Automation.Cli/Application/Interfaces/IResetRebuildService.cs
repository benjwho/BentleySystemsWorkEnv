using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Application.Interfaces;

internal interface IResetRebuildService
{
    CommandResult ResetRebuild(CommandContext context, ResetRebuildOptions options);
}

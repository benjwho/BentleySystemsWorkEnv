using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Application.Interfaces;

internal interface IRepairService
{
    CommandResult RepairProgramData(CommandContext context, RepairProgramDataOptions options);
}

using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Application.Interfaces;

internal interface IPickerAuditService
{
    CommandResult RunAudit(CommandContext context, AuditPickerOptions options);
}

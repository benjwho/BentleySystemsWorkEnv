using INWC.Automation.Cli.Application.Interfaces;
using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Application.UseCases;

internal sealed class AuditPickerUseCase : ICommandUseCase<AuditPickerOptions, CommandResult>
{
    private readonly IPickerAuditService _pickerAuditService;

    public AuditPickerUseCase(IPickerAuditService pickerAuditService)
    {
        _pickerAuditService = pickerAuditService;
    }

    public CommandResult Execute(CommandContext context, AuditPickerOptions options)
    {
        return _pickerAuditService.RunAudit(context, options);
    }
}

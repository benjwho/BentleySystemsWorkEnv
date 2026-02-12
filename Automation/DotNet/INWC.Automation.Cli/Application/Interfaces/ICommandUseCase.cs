using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Application.Interfaces;

internal interface ICommandUseCase<in TOptions, TResult>
    where TOptions : ICommandOptions
{
    TResult Execute(CommandContext context, TOptions options);
}

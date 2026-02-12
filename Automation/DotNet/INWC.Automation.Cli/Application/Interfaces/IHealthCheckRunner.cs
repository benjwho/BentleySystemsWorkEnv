using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Application.Interfaces;

internal interface IHealthCheckRunner
{
    IReadOnlyList<NamedStatus> Run(
        CommandContext context,
        CheckFullHealthOptions options,
        string executablePath,
        IReadOnlyList<string> executablePrefixArgs,
        string logPath);
}

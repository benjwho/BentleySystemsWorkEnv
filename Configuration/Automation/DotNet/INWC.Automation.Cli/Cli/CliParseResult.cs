using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Cli;

internal sealed record CliParseResult(bool ShowHelp, CliInvocation? Invocation, string? Error)
{
    public static CliParseResult Help() => new(true, null, null);
    public static CliParseResult Failure(string error) => new(false, null, error);
    public static CliParseResult Success(CliInvocation invocation) => new(false, invocation, null);
}

namespace INWC.Automation.Cli.Domain.Models;

internal sealed record CheckRecord(
    string Scope,
    string Check,
    string Target,
    bool Ok,
    string Detail = "");

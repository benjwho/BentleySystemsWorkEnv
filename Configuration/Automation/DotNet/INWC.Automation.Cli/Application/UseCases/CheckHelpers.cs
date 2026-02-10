using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Application.UseCases;

internal static class CheckHelpers
{
    public static void Add(List<CheckRecord> bucket, string scope, string check, string target, bool ok, string detail = "")
    {
        bucket.Add(new CheckRecord(scope, check, target, ok, detail));
    }

    public static bool HasFailure(IEnumerable<CheckRecord> checks)
    {
        return checks.Any(c => !c.Ok);
    }

    public static string ToForwardSlashPath(string value)
    {
        return value.Replace('\\', '/');
    }
}

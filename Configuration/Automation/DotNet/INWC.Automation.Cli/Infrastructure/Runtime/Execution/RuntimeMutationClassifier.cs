using INWC.Automation.Cli.Domain.Runtime;

namespace INWC.Automation.Cli.Infrastructure.Runtime.Execution;

internal static class RuntimeMutationClassifier
{
    public static bool IsMutating(RuntimeActionRule action)
    {
        if (!string.Equals(action.Type, "cli", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var args = action.CliArgs.Select(a => a.Trim()).Where(a => a.Length > 0).ToList();
        if (args.Count < 2)
        {
            return false;
        }

        if (args.Any(a => a.Equals("--what-if", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var a0 = args[0];
        var a1 = args[1];

        if (a0.Equals("fix", StringComparison.OrdinalIgnoreCase)
            && a1.Equals("picker-roots", StringComparison.OrdinalIgnoreCase)
            && args.Any(a => a.Equals("--apply", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (a0.Equals("detect", StringComparison.OrdinalIgnoreCase)
            && a1.Equals("integrations", StringComparison.OrdinalIgnoreCase)
            && args.Any(a => a.Equals("--apply", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (a0.Equals("repair", StringComparison.OrdinalIgnoreCase)
            && a1.Equals("programdata", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (a0.Equals("backup", StringComparison.OrdinalIgnoreCase)
            && a1.Equals("restore", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (a0.Equals("reset", StringComparison.OrdinalIgnoreCase)
            && a1.Equals("rebuild", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}

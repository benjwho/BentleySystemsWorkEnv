using INWC.Automation.Cli.Domain.Models;
using INWC.Automation.Cli.Infrastructure.Processes;

namespace INWC.Automation.Cli.Compatibility;

internal interface ILegacyScriptBridge
{
    CommandResult Invoke(CommandContext context, string scriptName, IReadOnlyList<string> legacyArgs);
}

internal sealed class LegacyScriptBridge : ILegacyScriptBridge
{
    private readonly IProcessRunner _processRunner;
    private readonly IExitCodePolicy _exitCodePolicy;

    public LegacyScriptBridge(IProcessRunner processRunner, IExitCodePolicy exitCodePolicy)
    {
        _processRunner = processRunner;
        _exitCodePolicy = exitCodePolicy;
    }

    public CommandResult Invoke(CommandContext context, string scriptName, IReadOnlyList<string> legacyArgs)
    {
        var scriptPath = Path.Combine(
            context.Global.TechRoot,
            "Configuration",
            "Automation",
            "PowerShell",
            "Legacy",
            scriptName);

        if (!File.Exists(scriptPath))
        {
            return new CommandResult
            {
                ExitCode = _exitCodePolicy.MissingDependency,
                Message = $"Legacy script not found: {scriptPath}"
            };
        }

        var args = new List<string>
        {
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-Command",
            BuildPowerShellCommand(scriptPath, legacyArgs)
        };

        var run = _processRunner.Run("powershell.exe", args, context.Global.TechRoot);

        if (!string.IsNullOrWhiteSpace(run.StdOut))
        {
            Console.Write(run.StdOut);
        }

        if (!string.IsNullOrWhiteSpace(run.StdErr))
        {
            Console.Error.Write(run.StdErr);
        }

        return new CommandResult
        {
            ExitCode = run.ExitCode,
            Message = run.ExitCode == 0 ? "Legacy command completed." : "Legacy command failed."
        };
    }

    private static string BuildPowerShellCommand(string scriptPath, IReadOnlyList<string> legacyArgs)
    {
        var pieces = new List<string> { "&", Quote(scriptPath) };
        foreach (var arg in legacyArgs)
        {
            if (arg.StartsWith("-", StringComparison.Ordinal)
                || (arg.StartsWith("@(", StringComparison.Ordinal) && arg.EndsWith(")", StringComparison.Ordinal)))
            {
                pieces.Add(arg);
            }
            else
            {
                pieces.Add(Quote(arg));
            }
        }

        return string.Join(" ", pieces);
    }

    private static string Quote(string value)
    {
        return "'" + value.Replace("'", "''") + "'";
    }
}

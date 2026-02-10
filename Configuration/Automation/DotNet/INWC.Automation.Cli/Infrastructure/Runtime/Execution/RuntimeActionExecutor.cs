using INWC.Automation.Cli.Application;
using INWC.Automation.Cli.Cli;
using INWC.Automation.Cli.Domain.Models;
using INWC.Automation.Cli.Domain.Runtime;
using INWC.Automation.Cli.Infrastructure.Config;
using INWC.Automation.Cli.Infrastructure.Processes;

namespace INWC.Automation.Cli.Infrastructure.Runtime.Execution;

internal sealed class RuntimeActionExecutor
{
    private readonly RuntimePathProvider _pathProvider;
    private readonly IConfigFileReader _configReader;
    private readonly IProcessRunner _processRunner;

    public RuntimeActionExecutor(RuntimePathProvider pathProvider, IConfigFileReader configReader, IProcessRunner processRunner)
    {
        _pathProvider = pathProvider;
        _configReader = configReader;
        _processRunner = processRunner;
    }

    public RuntimeActionExecutionResult Execute(CommandContext context, RuntimeActionIntent intent)
    {
        if (string.Equals(intent.ActionType, "cli", StringComparison.OrdinalIgnoreCase))
        {
            return ExecuteCli(context, intent);
        }

        if (string.Equals(intent.ActionType, "script.python", StringComparison.OrdinalIgnoreCase))
        {
            return ExecutePythonScript(context, intent);
        }

        return new RuntimeActionExecutionResult
        {
            ExitCode = 1,
            Message = $"Unsupported runtime action type: {intent.ActionType}"
        };
    }

    private RuntimeActionExecutionResult ExecuteCli(CommandContext context, RuntimeActionIntent intent)
    {
        var args = new List<string>(intent.CliArgs);
        if (!args.Any(a => a.Equals("--tech-root", StringComparison.OrdinalIgnoreCase)))
        {
            args.Add("--tech-root");
            args.Add(context.Global.TechRoot);
        }

        var parse = CliParser.Parse(args.ToArray());
        if (!string.IsNullOrWhiteSpace(parse.Error) || parse.Invocation is null)
        {
            return new RuntimeActionExecutionResult
            {
                ExitCode = 1,
                Message = "Failed to parse runtime CLI action: " + (parse.Error ?? "unknown parse error")
            };
        }

        if (IsRuntimeCommand(parse.Invocation.CommandId))
        {
            return new RuntimeActionExecutionResult
            {
                ExitCode = 1,
                Message = "Runtime actions cannot recursively invoke runtime commands."
            };
        }

        try
        {
            var dispatcher = new CommandDispatcher();
            var result = dispatcher.Execute(parse.Invocation);
            return new RuntimeActionExecutionResult
            {
                ExitCode = result.ExitCode,
                Message = result.Message,
                Artifacts = result.Artifacts,
                Data = result.Data
            };
        }
        catch (Exception ex)
        {
            return new RuntimeActionExecutionResult
            {
                ExitCode = 1,
                Message = "CLI runtime action failed: " + ex.Message
            };
        }
    }

    private RuntimeActionExecutionResult ExecutePythonScript(CommandContext context, RuntimeActionIntent intent)
    {
        var resolvedScript = ResolveScriptPath(intent.ScriptPath);
        if (resolvedScript is null)
        {
            return new RuntimeActionExecutionResult
            {
                ExitCode = 1,
                Message = "Script path is missing or outside the runtime whitelist."
            };
        }

        var pythonExe = ResolvePythonExe(context.Global.TechRoot);
        var args = new List<string> { resolvedScript };
        args.AddRange(intent.ScriptArgs);
        var run = _processRunner.Run(pythonExe, args, context.Global.TechRoot);
        return new RuntimeActionExecutionResult
        {
            ExitCode = run.ExitCode,
            Message = run.ExitCode == 0
                ? "Python runtime action completed."
                : "Python runtime action failed.",
            StdOut = run.StdOut,
            StdErr = run.StdErr
        };
    }

    private string ResolvePythonExe(string techRoot)
    {
        var pythonCfg = Path.Combine(techRoot, "Configuration", "Organization", "ByFunction", "Automation", "Python_Integration.cfg");
        var configured = _configReader.GetCfgVarValue(pythonCfg, "PYTHON_EXE");
        var normalized = _configReader.NormalizeWindowsPath(configured);
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        return "python";
    }

    private string? ResolveScriptPath(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var full = Path.IsPathRooted(raw)
            ? Path.GetFullPath(raw)
            : Path.GetFullPath(Path.Combine(_pathProvider.TechRoot, raw));

        if (!File.Exists(full) || !full.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var skillsRoot = Path.GetFullPath(Path.Combine(_pathProvider.TechRoot, ".agents", "skills")) + Path.DirectorySeparatorChar;
        var automationPythonRoot = Path.GetFullPath(Path.Combine(_pathProvider.TechRoot, "Configuration", "Automation", "Python")) + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(full);
        if (candidate.StartsWith(skillsRoot, StringComparison.OrdinalIgnoreCase)
            && candidate.Contains(Path.DirectorySeparatorChar + "scripts" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return candidate;
        }

        if (candidate.StartsWith(automationPythonRoot, StringComparison.OrdinalIgnoreCase))
        {
            return candidate;
        }

        return null;
    }

    private static bool IsRuntimeCommand(CommandId commandId)
    {
        return commandId switch
        {
            CommandId.RuntimeAgentStart => true,
            CommandId.RuntimeAgentInstall => true,
            CommandId.RuntimeAgentUninstall => true,
            CommandId.RuntimeServiceRun => true,
            CommandId.RuntimeServiceInstall => true,
            CommandId.RuntimeServiceStart => true,
            CommandId.RuntimeServiceStop => true,
            CommandId.RuntimeServiceUninstall => true,
            CommandId.RuntimeServiceStatus => true,
            CommandId.RuntimeQueueList => true,
            CommandId.RuntimeApprove => true,
            CommandId.RuntimeReject => true,
            CommandId.RuntimeTrigger => true,
            _ => false
        };
    }
}

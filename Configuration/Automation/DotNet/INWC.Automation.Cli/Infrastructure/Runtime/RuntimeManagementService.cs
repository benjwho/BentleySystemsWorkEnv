using INWC.Automation.Cli.Infrastructure.Processes;
using System.Text;

namespace INWC.Automation.Cli.Infrastructure.Runtime;

internal sealed class RuntimeManagementService
{
    private readonly RuntimePathProvider _pathProvider;
    private readonly IProcessRunner _processRunner;

    public RuntimeManagementService(RuntimePathProvider pathProvider, IProcessRunner processRunner)
    {
        _pathProvider = pathProvider;
        _processRunner = processRunner;
    }

    public RuntimeManagementResult InstallAgentTask(string? rulesPath)
    {
        var self = RuntimeSelfInvocation.Resolve();
        var args = new List<string>
        {
            "runtime", "agent", "start",
            "--tech-root", _pathProvider.TechRoot
        };

        if (!string.IsNullOrWhiteSpace(rulesPath))
        {
            args.Add("--rules-path");
            args.Add(rulesPath);
        }

        var launcherScript = WriteAgentLauncherScript(self, args);
        var commandLine = BuildPowerShellFileInvocation(launcherScript);
        var run = _processRunner.Run("schtasks.exe",
        [
            "/Create",
            "/TN", RuntimeConstants.AgentTaskName,
            "/SC", "ONLOGON",
            "/RL", "LIMITED",
            "/F",
            "/TR", commandLine
        ]);

        return ToResult(run, run.ExitCode == 0
            ? "Runtime agent scheduled task installed."
            : "Failed to install runtime agent scheduled task.");
    }

    public RuntimeManagementResult UninstallAgentTask()
    {
        var run = _processRunner.Run("schtasks.exe",
        [
            "/Delete",
            "/TN", RuntimeConstants.AgentTaskName,
            "/F"
        ]);

        if (run.ExitCode != 0
            && (run.StdErr.Contains("cannot find the file", StringComparison.OrdinalIgnoreCase)
                || run.StdErr.Contains("cannot find", StringComparison.OrdinalIgnoreCase)))
        {
            return new RuntimeManagementResult
            {
                ExitCode = 0,
                Message = "Runtime agent scheduled task already absent.",
                StdOut = run.StdOut,
                StdErr = run.StdErr
            };
        }

        return ToResult(run, run.ExitCode == 0
            ? "Runtime agent scheduled task removed."
            : "Failed to remove runtime agent scheduled task.");
    }

    public RuntimeManagementResult InstallService(string? rulesPath)
    {
        var self = RuntimeSelfInvocation.Resolve();
        var args = new List<string>
        {
            "runtime", "service", "run",
            "--tech-root", _pathProvider.TechRoot
        };

        if (!string.IsNullOrWhiteSpace(rulesPath))
        {
            args.Add("--rules-path");
            args.Add(rulesPath);
        }

        var commandLine = self.BuildCommandLine(args);
        var create = _processRunner.Run("sc.exe",
        [
            "create",
            RuntimeConstants.ServiceName,
            "binPath=", commandLine,
            "start=", "auto",
            "DisplayName=", RuntimeConstants.ServiceDisplayName
        ]);

        if (create.ExitCode != 0)
        {
            return ToResult(create, "Failed to install runtime service.");
        }

        var description = _processRunner.Run("sc.exe",
        [
            "description",
            RuntimeConstants.ServiceName,
            "INWC RuntimeBridge service host (queues periodic runtime intents)."
        ]);

        var merged = new ProcessRunResult(
            description.ExitCode,
            create.StdOut + Environment.NewLine + description.StdOut,
            create.StdErr + Environment.NewLine + description.StdErr);

        return ToResult(merged, description.ExitCode == 0
            ? "Runtime service installed."
            : "Runtime service installed with warnings.");
    }

    public RuntimeManagementResult UninstallService()
    {
        _processRunner.Run("sc.exe", ["stop", RuntimeConstants.ServiceName]);
        var delete = _processRunner.Run("sc.exe", ["delete", RuntimeConstants.ServiceName]);
        return ToResult(delete, delete.ExitCode == 0
            ? "Runtime service removed."
            : "Failed to remove runtime service.");
    }

    public RuntimeManagementResult StartService()
    {
        var run = _processRunner.Run("sc.exe", ["start", RuntimeConstants.ServiceName]);
        return ToResult(run, run.ExitCode == 0 ? "Runtime service start requested." : "Failed to start runtime service.");
    }

    public RuntimeManagementResult StopService()
    {
        var run = _processRunner.Run("sc.exe", ["stop", RuntimeConstants.ServiceName]);
        return ToResult(run, run.ExitCode == 0 ? "Runtime service stop requested." : "Failed to stop runtime service.");
    }

    public RuntimeManagementResult QueryService()
    {
        var run = _processRunner.Run("sc.exe", ["query", RuntimeConstants.ServiceName]);
        return ToResult(run, run.ExitCode == 0 ? "Runtime service status queried." : "Failed to query runtime service.");
    }

    private static RuntimeManagementResult ToResult(ProcessRunResult run, string message)
    {
        return new RuntimeManagementResult
        {
            ExitCode = run.ExitCode,
            Message = message,
            StdOut = run.StdOut,
            StdErr = run.StdErr
        };
    }

    private string WriteAgentLauncherScript(RuntimeSelfInvocation self, IReadOnlyList<string> args)
    {
        _pathProvider.EnsureServiceStateDirectory();
        var launcherPath = Path.Combine(_pathProvider.ServiceStateRoot, "agent-launch.ps1");

        static string Q(string value) => "'" + value.Replace("'", "''") + "'";

        var sb = new StringBuilder();
        sb.AppendLine("$ErrorActionPreference = 'Stop'");
        sb.AppendLine("$file = " + Q(self.FileName));
        sb.AppendLine("$arguments = @(");

        var allArgs = new List<string>();
        allArgs.AddRange(self.PrefixArgs);
        allArgs.AddRange(args);
        for (var i = 0; i < allArgs.Count; i++)
        {
            var suffix = i < allArgs.Count - 1 ? "," : string.Empty;
            sb.AppendLine("  " + Q(allArgs[i]) + suffix);
        }

        sb.AppendLine(")");
        sb.AppendLine("& $file @arguments");
        sb.AppendLine("exit $LASTEXITCODE");

        File.WriteAllText(launcherPath, sb.ToString(), Encoding.ASCII);
        return launcherPath;
    }

    private static string BuildPowerShellFileInvocation(string scriptPath)
    {
        return $"powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"";
    }
}

internal sealed class RuntimeManagementResult
{
    public int ExitCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public string StdOut { get; init; } = string.Empty;
    public string StdErr { get; init; } = string.Empty;
}

using INWC.Automation.Cli.Application.Interfaces;
using INWC.Automation.Cli.Compatibility;
using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Application.UseCases;

internal sealed class CheckFullHealthUseCase : ICommandUseCase<CheckFullHealthOptions, CommandResult>
{
    private readonly IHealthCheckRunner _healthCheckRunner;
    private readonly IArtifactPathPolicy _artifactPathPolicy;
    private readonly IExitCodePolicy _exitCodePolicy;

    public CheckFullHealthUseCase(
        IHealthCheckRunner healthCheckRunner,
        IArtifactPathPolicy artifactPathPolicy,
        IExitCodePolicy exitCodePolicy)
    {
        _healthCheckRunner = healthCheckRunner;
        _artifactPathPolicy = artifactPathPolicy;
        _exitCodePolicy = exitCodePolicy;
    }

    public CommandResult Execute(CommandContext context, CheckFullHealthOptions options)
    {
        var logPath = _artifactPathPolicy.CreateTimestampedLogFile(context.Global.TechRoot, "FullHealthCheck", "txt");
        var executablePath = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot resolve current executable path.");
        var executablePrefixArgs = new List<string>();
        if (Path.GetFileName(executablePath).Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase))
        {
            var dllPath = Path.Combine(AppContext.BaseDirectory, "inwc-cli.dll");
            if (File.Exists(dllPath))
            {
                executablePrefixArgs.Add(dllPath);
            }
        }

        var statuses = _healthCheckRunner.Run(context, options, executablePath, executablePrefixArgs, logPath);
        var hasFailure = statuses.Any(s => !s.Ok);

        return new CommandResult
        {
            ExitCode = hasFailure ? _exitCodePolicy.Failure : _exitCodePolicy.Success,
            Message = hasFailure
                ? $"FAIL: {statuses.Count(s => !s.Ok)} check(s) failed."
                : "PASS: all checks passed.",
            Statuses = statuses,
            Artifacts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Log"] = logPath
            }
        };
    }
}

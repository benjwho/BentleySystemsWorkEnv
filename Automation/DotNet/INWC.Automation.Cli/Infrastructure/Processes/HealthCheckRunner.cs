using System.Text;
using INWC.Automation.Cli.Application.Interfaces;
using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Infrastructure.Processes;

internal sealed class HealthCheckRunner : IHealthCheckRunner
{
    private readonly IProcessRunner _processRunner;

    public HealthCheckRunner(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public IReadOnlyList<NamedStatus> Run(
        CommandContext context,
        CheckFullHealthOptions options,
        string executablePath,
        IReadOnlyList<string> executablePrefixArgs,
        string logPath)
    {
        var checks = new List<(string Name, List<string> Args)>
        {
            ("Environment", new List<string> { "check", "env" }),
            ("Integration", new List<string> { "check", "integration" }),
            ("Python Smoke", new List<string> { "smoke", "python" }),
            ("ProjectWise Smoke", new List<string> { "smoke", "projectwise" })
        };

        if (options.SkipPythonRuntimeTest)
        {
            checks[2].Args.Add("--skip-runtime-test");
        }

        if (options.SkipProjectWiseWriteTest)
        {
            checks[3].Args.Add("--skip-write-test");
        }

        var statuses = new List<NamedStatus>();
        var sb = new StringBuilder();
        sb.AppendLine("INWC Full Health Check");
        sb.AppendLine($"Timestamp: {DateTime.Now:s}");
        sb.AppendLine($"TechRoot: {context.Global.TechRoot}");
        sb.AppendLine($"SkipPythonRuntimeTest: {options.SkipPythonRuntimeTest}");
        sb.AppendLine($"SkipProjectWiseWriteTest: {options.SkipProjectWiseWriteTest}");

        foreach (var check in checks)
        {
            var args = new List<string>(check.Args)
            {
                "--tech-root", context.Global.TechRoot
            };
            if (executablePrefixArgs.Count > 0)
            {
                args.InsertRange(0, executablePrefixArgs);
            }

            if (context.Global.Verbose)
            {
                args.Add("--verbose");
            }

            if (context.Global.WhatIf)
            {
                args.Add("--what-if");
            }

            sb.AppendLine();
            sb.AppendLine($"=== {check.Name} ===");
            sb.AppendLine($"Command: {executablePath} {string.Join(" ", args)}");

            var run = _processRunner.Run(executablePath, args, context.Global.TechRoot);
            foreach (var line in run.AllLines())
            {
                sb.AppendLine(line);
            }

            sb.AppendLine($"ExitCode: {run.ExitCode}");
            statuses.Add(new NamedStatus(check.Name, run.ExitCode, run.ExitCode == 0));
        }

        sb.AppendLine();
        sb.AppendLine("=== Summary ===");
        foreach (var status in statuses)
        {
            sb.AppendLine($"{status.Name}: ExitCode={status.ExitCode}; OK={status.Ok}");
        }

        File.WriteAllText(logPath, sb.ToString(), Encoding.ASCII);
        Console.Write(sb.ToString());

        return statuses;
    }
}

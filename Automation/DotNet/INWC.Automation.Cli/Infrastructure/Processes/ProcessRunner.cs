using System.Diagnostics;

namespace INWC.Automation.Cli.Infrastructure.Processes;

internal sealed record ProcessRunResult(int ExitCode, string StdOut, string StdErr)
{
    public IEnumerable<string> AllLines()
    {
        foreach (var line in (StdOut + Environment.NewLine + StdErr).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            if (!string.IsNullOrEmpty(line))
            {
                yield return line;
            }
        }
    }
}

internal interface IProcessRunner
{
    ProcessRunResult Run(string fileName, IReadOnlyList<string> args, string? workingDirectory = null);
}

internal sealed class ProcessRunner : IProcessRunner
{
    public ProcessRunResult Run(string fileName, IReadOnlyList<string> args, string? workingDirectory = null)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? Environment.CurrentDirectory : workingDirectory
        };

        foreach (var argument in args)
        {
            psi.ArgumentList.Add(argument);
        }

        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start process: {fileName}");
        var stdOut = process.StandardOutput.ReadToEnd();
        var stdErr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ProcessRunResult(process.ExitCode, stdOut, stdErr);
    }
}

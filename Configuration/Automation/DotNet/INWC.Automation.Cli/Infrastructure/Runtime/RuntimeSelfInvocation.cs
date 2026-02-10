namespace INWC.Automation.Cli.Infrastructure.Runtime;

internal sealed class RuntimeSelfInvocation
{
    public RuntimeSelfInvocation(string fileName, IReadOnlyList<string> prefixArgs)
    {
        FileName = fileName;
        PrefixArgs = prefixArgs;
    }

    public string FileName { get; }
    public IReadOnlyList<string> PrefixArgs { get; }

    public static RuntimeSelfInvocation Resolve()
    {
        var processPath = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot resolve current process path.");
        if (Path.GetFileName(processPath).Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase))
        {
            var dllPath = Path.Combine(AppContext.BaseDirectory, "inwc-cli.dll");
            if (File.Exists(dllPath))
            {
                return new RuntimeSelfInvocation(processPath, [dllPath]);
            }
        }

        return new RuntimeSelfInvocation(processPath, []);
    }

    public string BuildCommandLine(IReadOnlyList<string> commandArgs)
    {
        var all = new List<string> { FileName };
        all.AddRange(PrefixArgs);
        all.AddRange(commandArgs);
        return string.Join(" ", all.Select(Quote));
    }

    private static string Quote(string arg)
    {
        if (string.IsNullOrWhiteSpace(arg))
        {
            return "\"\"";
        }

        if (arg.IndexOfAny([' ', '"', '\t']) < 0)
        {
            return arg;
        }

        return "\"" + arg.Replace("\"", "\\\"") + "\"";
    }
}

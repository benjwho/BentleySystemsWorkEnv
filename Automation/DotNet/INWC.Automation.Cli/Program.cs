using INWC.Automation.Cli.Application;
using INWC.Automation.Cli.Cli;
using INWC.Automation.Cli.Infrastructure.Output;

namespace INWC.Automation.Cli;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var parseResult = CliParser.Parse(args);
        if (parseResult.ShowHelp)
        {
            UsageWriter.Write();
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(parseResult.Error))
        {
            Console.Error.WriteLine("Argument error: " + parseResult.Error);
            Console.Error.WriteLine();
            UsageWriter.Write();
            return 2;
        }

        var invocation = parseResult.Invocation;
        if (invocation is null)
        {
            Console.Error.WriteLine("No command was parsed.");
            return 2;
        }

        if (!Directory.Exists(invocation.Global.TechRoot))
        {
            Console.Error.WriteLine("Tech root not found: " + invocation.Global.TechRoot);
            return 1;
        }

        try
        {
            var dispatcher = new CommandDispatcher();
            var result = dispatcher.Execute(invocation);
            var writer = new ResultWriter();
            writer.Write(result, invocation.Global);
            return result.ExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Unhandled error: " + ex.Message);
            return 1;
        }
    }
}

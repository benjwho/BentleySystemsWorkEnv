using System.Text.Json;
using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Infrastructure.Output;

internal interface IResultWriter
{
    void Write(CommandResult result, GlobalOptions options);
}

internal sealed class ResultWriter : IResultWriter
{
    public void Write(CommandResult result, GlobalOptions options)
    {
        if (options.Json)
        {
            var payload = new
            {
                exitCode = result.ExitCode,
                message = result.Message,
                checks = result.Checks,
                statuses = result.Statuses,
                artifacts = result.Artifacts,
                data = result.Data
            };

            Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        if (!string.IsNullOrWhiteSpace(result.Message))
        {
            Console.WriteLine(result.Message);
        }

        if (result.Checks.Count > 0)
        {
            Console.WriteLine();
            WriteChecksTable(result.Checks);
        }

        if (result.Statuses.Count > 0)
        {
            Console.WriteLine();
            WriteStatusesTable(result.Statuses);
        }

        if (result.Artifacts.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Artifacts:");
            foreach (var artifact in result.Artifacts)
            {
                Console.WriteLine($"  {artifact.Key}: {artifact.Value}");
            }
        }

        if (result.Data.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Data:");
            foreach (var entry in result.Data)
            {
                var rendered = entry.Value switch
                {
                    null => "null",
                    string s => s,
                    _ => JsonSerializer.Serialize(entry.Value)
                };
                Console.WriteLine($"  {entry.Key}: {rendered}");
            }
        }
    }

    private static void WriteChecksTable(IReadOnlyList<CheckRecord> rows)
    {
        var scopeWidth = Math.Max("Scope".Length, rows.Max(r => r.Scope.Length));
        var checkWidth = Math.Max("Check".Length, rows.Max(r => r.Check.Length));
        var targetWidth = Math.Max("Target".Length, rows.Max(r => r.Target.Length));
        var okWidth = "OK".Length;

        Console.WriteLine($"{Pad("Scope", scopeWidth)}  {Pad("Check", checkWidth)}  {Pad("Target", targetWidth)}  OK  Detail");
        Console.WriteLine($"{new string('-', scopeWidth)}  {new string('-', checkWidth)}  {new string('-', targetWidth)}  --  ------");

        foreach (var row in rows)
        {
            var ok = row.Ok ? "Y" : "N";
            Console.WriteLine($"{Pad(row.Scope, scopeWidth)}  {Pad(row.Check, checkWidth)}  {Pad(row.Target, targetWidth)}  {ok}   {row.Detail}");
        }
    }

    private static void WriteStatusesTable(IReadOnlyList<NamedStatus> rows)
    {
        var checkWidth = Math.Max("Check".Length, rows.Max(r => r.Name.Length));
        Console.WriteLine($"{Pad("Check", checkWidth)}  ExitCode  OK");
        Console.WriteLine($"{new string('-', checkWidth)}  --------  --");
        foreach (var row in rows)
        {
            Console.WriteLine($"{Pad(row.Name, checkWidth)}  {row.ExitCode,8}  {(row.Ok ? "Y" : "N")}");
        }
    }

    private static string Pad(string value, int width)
    {
        if (value.Length >= width)
        {
            return value;
        }

        return value.PadRight(width);
    }
}

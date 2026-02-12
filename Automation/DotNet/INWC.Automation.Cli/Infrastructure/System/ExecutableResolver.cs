using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Infrastructure.System;

internal interface IExecutableResolver
{
    string Resolve(AppDefinition app);
}

internal sealed class ExecutableResolver : IExecutableResolver
{
    public string Resolve(AppDefinition app)
    {
        foreach (var candidate in app.Candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var roots = new[] { @"C:\Program Files\Bentley", @"C:\Program Files (x86)\Bentley" };
        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var pattern in app.Patterns)
            {
                try
                {
                    var found = Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(found))
                    {
                        return found;
                    }
                }
                catch
                {
                    // Continue scanning next pattern/root.
                }
            }
        }

        throw new InvalidOperationException($"Could not find executable for {app.Name}");
    }
}

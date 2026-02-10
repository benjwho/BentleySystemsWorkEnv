namespace INWC.Automation.Cli.Infrastructure.System;

internal static class TechRootResolver
{
    public static string Resolve(string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            return Path.GetFullPath(requested);
        }

        var probes = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var probe in probes)
        {
            var resolved = FindRootFrom(probe);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                return resolved;
            }
        }

        return Directory.GetCurrentDirectory();
    }

    private static string? FindRootFrom(string start)
    {
        var dir = new DirectoryInfo(Path.GetFullPath(start));
        while (dir is not null)
        {
            var marker = Path.Combine(dir.FullName, "Configuration", "WorkSpaceSetup.cfg");
            if (File.Exists(marker))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}

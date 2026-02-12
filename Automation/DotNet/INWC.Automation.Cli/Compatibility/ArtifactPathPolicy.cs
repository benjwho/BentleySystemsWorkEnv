namespace INWC.Automation.Cli.Compatibility;

internal interface IArtifactPathPolicy
{
    string EnsureLogRoot(string techRoot);
    string CreateTimestampedLogFile(string techRoot, string prefix, string extension = "txt");
    string CreateTimestampedRunFolder(string techRoot, string prefix);
}

internal sealed class ArtifactPathPolicy : IArtifactPathPolicy
{
    public string EnsureLogRoot(string techRoot)
    {
        var logRoot = Path.Combine(techRoot, "Logs", "NWC_Rehab");
        Directory.CreateDirectory(logRoot);
        return logRoot;
    }

    public string CreateTimestampedLogFile(string techRoot, string prefix, string extension = "txt")
    {
        var root = EnsureLogRoot(techRoot);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        return Path.Combine(root, $"{prefix}_{stamp}.{extension.TrimStart('.')}");
    }

    public string CreateTimestampedRunFolder(string techRoot, string prefix)
    {
        var root = EnsureLogRoot(techRoot);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var path = Path.Combine(root, $"{prefix}_{stamp}");
        Directory.CreateDirectory(path);
        return path;
    }
}

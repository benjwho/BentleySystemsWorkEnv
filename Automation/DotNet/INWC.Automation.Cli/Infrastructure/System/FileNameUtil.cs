namespace INWC.Automation.Cli.Infrastructure.System;

internal static class FileNameUtil
{
    public static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var chars = value.Select(ch => invalid.Contains(ch) || ch == ' ' || ch == ':' ? '_' : ch).ToArray();
        return new string(chars).Trim('_');
    }
}

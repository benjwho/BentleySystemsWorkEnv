using System.Text.RegularExpressions;

namespace INWC.Automation.Cli.Infrastructure.Config;

internal interface IConfigFileReader
{
    string? GetCfgVarValue(string path, string variable);
    string? NormalizeCfgRoot(string? value);
    string? NormalizeWindowsPath(string? value);
    string? ResolveCfgTokens(string? value, IReadOnlyDictionary<string, string> tokens);
}

internal sealed class ConfigFileReader : IConfigFileReader
{
    public string? GetCfgVarValue(string path, string variable)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var expression = new Regex("^\\s*" + Regex.Escape(variable) + "\\s*=\\s*(.*?)\\s*$", RegexOptions.IgnoreCase);
        string? last = null;

        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("#") || trimmed.StartsWith(";"))
            {
                continue;
            }

            var match = expression.Match(line);
            if (match.Success)
            {
                last = match.Groups[1].Value.Trim();
            }
        }

        return last;
    }

    public string? NormalizeCfgRoot(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().Trim('"', '\'').Replace('\\', '/').TrimEnd('/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized + "/";
    }

    public string? NormalizeWindowsPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().Trim('"', '\'').Replace('/', '\\');
    }

    public string? ResolveCfgTokens(string? value, IReadOnlyDictionary<string, string> tokens)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var resolved = value;
        for (var i = 0; i < 8; i++)
        {
            var next = Regex.Replace(
                resolved,
                "\\$\\(([A-Za-z0-9_]+)\\)",
                match =>
                {
                    var tokenName = match.Groups[1].Value;
                    return tokens.TryGetValue(tokenName, out var tokenValue) ? tokenValue : match.Value;
                });

            if (string.Equals(next, resolved, StringComparison.Ordinal))
            {
                break;
            }

            resolved = next;
        }

        return NormalizeWindowsPath(resolved);
    }
}

using System.Text.RegularExpressions;

namespace INWC.Automation.Cli.Infrastructure.System;

internal static class TextFileMutator
{
    public static string SetManagedPickerBlock(string originalText, string managedBlock)
    {
        const string blockPattern = "(?ms)^\\s*#\\s*--- BEGIN INWC_PICKER_ROOTS \\(managed\\) ---\\s*\\r?\\n.*?^\\s*#\\s*--- END INWC_PICKER_ROOTS \\(managed\\) ---\\s*\\r?\\n?";

        var blockRegex = new Regex(blockPattern);
        var baseText = blockRegex.Replace(originalText, string.Empty, 1);
        baseText = baseText.TrimEnd('\r', '\n');

        if (string.IsNullOrWhiteSpace(baseText))
        {
            return managedBlock + "\r\n";
        }

        return baseText + "\r\n\r\n" + managedBlock + "\r\n";
    }

    public static string SetCfgVariableLine(string text, string variable, string value)
    {
        var assignment = $"{variable}={value}";
        var linePattern = $"(?m)^\\s*{Regex.Escape(variable)}\\s*=.*$";
        var lineRegex = new Regex(linePattern);

        if (lineRegex.IsMatch(text))
        {
            return lineRegex.Replace(text, assignment, 1);
        }

        const string generalPattern = "(?ms)^\\s*\\[General\\]\\s*\\r?\\n";
        var generalRegex = new Regex(generalPattern);
        if (generalRegex.IsMatch(text))
        {
            return generalRegex.Replace(text, "[General]\r\n" + assignment + "\r\n", 1);
        }

        var trimmed = text.TrimEnd('\r', '\n');
        if (string.IsNullOrEmpty(trimmed))
        {
            return "[General]\r\n" + assignment + "\r\n";
        }

        return "[General]\r\n" + assignment + "\r\n\r\n" + trimmed + "\r\n";
    }
}

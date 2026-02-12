using System.Text;

namespace INWC.Automation.Cli.Infrastructure.Audit;

internal interface IAuditLogger
{
    string LogPath { get; }
    void Info(string message);
    void Warn(string message);
    void Error(string message);
}

internal sealed class AuditLogger : IAuditLogger
{
    public string LogPath { get; }

    public AuditLogger(string logPath)
    {
        LogPath = logPath;
        var parent = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:s}] [{level}] {message}";
        Console.WriteLine(line);
        File.AppendAllText(LogPath, line + Environment.NewLine, Encoding.ASCII);
    }
}

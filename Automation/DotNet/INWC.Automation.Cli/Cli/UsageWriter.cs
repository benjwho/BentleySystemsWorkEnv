namespace INWC.Automation.Cli.Cli;

internal static class UsageWriter
{
    public static void Write()
    {
        Console.WriteLine("inwc-cli - INWC automation CLI");
        Console.WriteLine();
        Console.WriteLine("Global options:");
        Console.WriteLine("  --tech-root <path>    Tech root path (auto-detected when omitted)");
        Console.WriteLine("  --json                Emit machine-readable JSON output");
        Console.WriteLine("  --what-if             Dry-run for mutating commands");
        Console.WriteLine("  --verbose             Verbose output");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  check env [--product-config-root <path> ...]");
        Console.WriteLine("  check integration");
        Console.WriteLine("  check full-health [--skip-python-runtime-test] [--skip-projectwise-write-test]");
        Console.WriteLine("  fix picker-roots [--apply]");
        Console.WriteLine("  audit picker [--autopilot] [--autopilot-file-path <path>] [--skip-launch] [--keep-apps-open]");
        Console.WriteLine("               [--picker-failed] [--wait-seconds <n>] [--readiness-settle-seconds <n>]");
        Console.WriteLine("               [--user-action-seconds <n>] [--wait-for-enter-before-capture] [--non-interactive]");
        Console.WriteLine("  smoke python [--skip-runtime-test]");
        Console.WriteLine("  smoke projectwise [--skip-write-test]");
        Console.WriteLine("  detect integrations [--apply]");
        Console.WriteLine("  repair programdata [--product-config-root <path> ...]");
        Console.WriteLine("  backup create [--include-user-prefs] [--product-config-root <path> ...]");
        Console.WriteLine("  backup restore --from <path>");
        Console.WriteLine("  reset rebuild [--reset-user-prefs] [--restore-oldest-cfg-backup] [--include-logs-in-backup] [--product-config-root <path> ...]");
        Console.WriteLine("  init workspace-report");
        Console.WriteLine("  runtime agent start [--rules-path <path>] [--poll-seconds <n>] [--once]");
        Console.WriteLine("  runtime agent install");
        Console.WriteLine("  runtime agent uninstall");
        Console.WriteLine("  runtime service run [--rules-path <path>]");
        Console.WriteLine("  runtime service install|start|stop|uninstall|status");
        Console.WriteLine("  runtime queue list");
        Console.WriteLine("  runtime approve <action-id> [--note <text>]");
        Console.WriteLine("  runtime reject <action-id> [--note <text>]");
        Console.WriteLine("  runtime trigger <event-name> [--payload-json <path>]");
    }
}

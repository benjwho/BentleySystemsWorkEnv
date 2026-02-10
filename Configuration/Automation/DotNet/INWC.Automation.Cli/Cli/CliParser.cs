using INWC.Automation.Cli.Catalog;
using INWC.Automation.Cli.Domain.Models;
using INWC.Automation.Cli.Infrastructure.System;

namespace INWC.Automation.Cli.Cli;

internal static class CliParser
{
    public static CliParseResult Parse(string[] args)
    {
        try
        {
            var global = ParseGlobal(args, out var commandArgs, out var showHelp);
            if (showHelp || commandArgs.Count == 0)
            {
                return CliParseResult.Help();
            }

            var invocation = ParseInvocation(global, commandArgs);
            return CliParseResult.Success(invocation);
        }
        catch (ArgumentException ex)
        {
            return CliParseResult.Failure(ex.Message);
        }
    }

    private static GlobalOptions ParseGlobal(string[] args, out List<string> remaining, out bool showHelp)
    {
        remaining = new List<string>();
        showHelp = false;

        string? techRoot = null;
        var json = false;
        var whatIf = false;
        var verbose = false;

        for (var i = 0; i < args.Length; i++)
        {
            var token = args[i];
            switch (token.ToLowerInvariant())
            {
                case "--tech-root":
                    techRoot = ReadRequiredValue(args, ref i, token);
                    break;
                case "--json":
                    json = true;
                    break;
                case "--what-if":
                    whatIf = true;
                    break;
                case "--verbose":
                    verbose = true;
                    break;
                case "-h":
                case "--help":
                case "/?":
                    showHelp = true;
                    break;
                default:
                    remaining.Add(token);
                    break;
            }
        }

        var resolvedRoot = TechRootResolver.Resolve(techRoot);
        return new GlobalOptions(resolvedRoot, json, whatIf, verbose);
    }

    private static CliInvocation ParseInvocation(GlobalOptions global, List<string> args)
    {
        var head = args[0].ToLowerInvariant();

        return head switch
        {
            "check" => ParseCheckCommand(global, args),
            "fix" => ParseFixCommand(global, args),
            "audit" => ParseAuditCommand(global, args),
            "smoke" => ParseSmokeCommand(global, args),
            "detect" => ParseDetectCommand(global, args),
            "repair" => ParseRepairCommand(global, args),
            "backup" => ParseBackupCommand(global, args),
            "reset" => ParseResetCommand(global, args),
            "init" => ParseInitCommand(global, args),
            "runtime" => ParseRuntimeCommand(global, args),
            _ => throw new ArgumentException($"Unknown command group: {args[0]}")
        };
    }

    private static CliInvocation ParseCheckCommand(GlobalOptions global, List<string> args)
    {
        EnsureVerb(args, "check");
        if (args.Count < 2)
        {
            throw new ArgumentException("Missing check command. Expected one of: env, integration, full-health.");
        }

        var sub = args[1].ToLowerInvariant();
        var tail = args.Skip(2).ToList();

        switch (sub)
        {
            case "env":
            {
                var roots = ConsumeMultiValueOption(tail, "--product-config-root");
                var options = new CheckEnvOptions(roots.Count > 0 ? roots : ProductConfigCatalog.DefaultRoots.ToArray());
                return Build(global, CommandId.CheckEnv, options, tail);
            }
            case "integration":
                return Build(global, CommandId.CheckIntegration, new CheckIntegrationOptions(), tail);
            case "full-health":
            {
                var options = new CheckFullHealthOptions(
                    SkipPythonRuntimeTest: ConsumeFlag(tail, "--skip-python-runtime-test"),
                    SkipProjectWiseWriteTest: ConsumeFlag(tail, "--skip-projectwise-write-test"));
                return Build(global, CommandId.CheckFullHealth, options, tail);
            }
            default:
                throw new ArgumentException($"Unknown check command: {args[1]}");
        }
    }

    private static CliInvocation ParseFixCommand(GlobalOptions global, List<string> args)
    {
        EnsureVerb(args, "fix", "picker-roots");
        var tail = args.Skip(2).ToList();
        var options = new FixPickerRootsOptions(Apply: ConsumeFlag(tail, "--apply"));
        return Build(global, CommandId.FixPickerRoots, options, tail);
    }

    private static CliInvocation ParseAuditCommand(GlobalOptions global, List<string> args)
    {
        EnsureVerb(args, "audit", "picker");
        var tail = args.Skip(2).ToList();

        var options = new AuditPickerOptions(
            AutoPilot: ConsumeFlag(tail, "--autopilot"),
            AutoPilotFilePath: ConsumeOptionalValue(tail, "--autopilot-file-path"),
            SkipLaunch: ConsumeFlag(tail, "--skip-launch"),
            KeepAppsOpen: ConsumeFlag(tail, "--keep-apps-open"),
            PickerFailed: ConsumeFlag(tail, "--picker-failed"),
            WaitSeconds: ConsumeOptionalInt(tail, "--wait-seconds", 25),
            ReadinessSettleSeconds: ConsumeOptionalInt(tail, "--readiness-settle-seconds", 3),
            UserActionSeconds: ConsumeOptionalInt(tail, "--user-action-seconds", 20),
            WaitForEnterBeforeCapture: ConsumeFlag(tail, "--wait-for-enter-before-capture"),
            NonInteractive: ConsumeFlag(tail, "--non-interactive"));

        ValidateNonNegative(options.WaitSeconds, "--wait-seconds");
        ValidateNonNegative(options.ReadinessSettleSeconds, "--readiness-settle-seconds");
        ValidateNonNegative(options.UserActionSeconds, "--user-action-seconds");

        return Build(global, CommandId.AuditPicker, options, tail);
    }

    private static CliInvocation ParseSmokeCommand(GlobalOptions global, List<string> args)
    {
        EnsureVerb(args, "smoke");
        if (args.Count < 2)
        {
            throw new ArgumentException("Missing smoke command. Expected one of: python, projectwise.");
        }

        var sub = args[1].ToLowerInvariant();
        var tail = args.Skip(2).ToList();

        return sub switch
        {
            "python" => Build(global, CommandId.SmokePython, new SmokePythonOptions(SkipRuntimeTest: ConsumeFlag(tail, "--skip-runtime-test")), tail),
            "projectwise" => Build(global, CommandId.SmokeProjectWise, new SmokeProjectWiseOptions(SkipWriteTest: ConsumeFlag(tail, "--skip-write-test")), tail),
            _ => throw new ArgumentException($"Unknown smoke command: {args[1]}")
        };
    }

    private static CliInvocation ParseDetectCommand(GlobalOptions global, List<string> args)
    {
        EnsureVerb(args, "detect", "integrations");
        var tail = args.Skip(2).ToList();
        var options = new DetectIntegrationsOptions(Apply: ConsumeFlag(tail, "--apply"));
        return Build(global, CommandId.DetectIntegrations, options, tail);
    }

    private static CliInvocation ParseRepairCommand(GlobalOptions global, List<string> args)
    {
        EnsureVerb(args, "repair", "programdata");
        var tail = args.Skip(2).ToList();
        var roots = ConsumeMultiValueOption(tail, "--product-config-root");
        var options = new RepairProgramDataOptions(
            roots.Count > 0 ? roots : ProductConfigCatalog.DefaultRoots.ToArray());

        return Build(global, CommandId.RepairProgramData, options, tail);
    }

    private static CliInvocation ParseBackupCommand(GlobalOptions global, List<string> args)
    {
        EnsureVerb(args, "backup");
        if (args.Count < 2)
        {
            throw new ArgumentException("Missing backup command. Expected one of: create, restore.");
        }

        var sub = args[1].ToLowerInvariant();
        var tail = args.Skip(2).ToList();

        switch (sub)
        {
            case "create":
            {
                var roots = ConsumeMultiValueOption(tail, "--product-config-root");
                var options = new BackupCreateOptions(
                    IncludeUserPrefs: ConsumeFlag(tail, "--include-user-prefs"),
                    ProductConfigRoots: roots.Count > 0 ? roots : ProductConfigCatalog.DefaultRoots.ToArray());
                return Build(global, CommandId.BackupCreate, options, tail);
            }
            case "restore":
                return Build(global, CommandId.BackupRestore, new BackupRestoreOptions(ConsumeRequiredValue(tail, "--from")), tail);
            default:
                throw new ArgumentException($"Unknown backup command: {args[1]}");
        }
    }

    private static CliInvocation ParseResetCommand(GlobalOptions global, List<string> args)
    {
        EnsureVerb(args, "reset", "rebuild");
        var tail = args.Skip(2).ToList();
        var roots = ConsumeMultiValueOption(tail, "--product-config-root");

        var options = new ResetRebuildOptions(
            ResetUserPrefs: ConsumeFlag(tail, "--reset-user-prefs"),
            RestoreOldestCfgBackup: ConsumeFlag(tail, "--restore-oldest-cfg-backup"),
            IncludeLogsInBackup: ConsumeFlag(tail, "--include-logs-in-backup"),
            ProductConfigRoots: roots.Count > 0 ? roots : ProductConfigCatalog.DefaultRoots.ToArray());

        return Build(global, CommandId.ResetRebuild, options, tail);
    }

    private static CliInvocation ParseInitCommand(GlobalOptions global, List<string> args)
    {
        EnsureVerb(args, "init", "workspace-report");
        var tail = args.Skip(2).ToList();
        return Build(global, CommandId.InitWorkspaceReport, new InitWorkspaceReportOptions(), tail);
    }

    private static CliInvocation ParseRuntimeCommand(GlobalOptions global, List<string> args)
    {
        EnsureVerb(args, "runtime");
        if (args.Count < 2)
        {
            throw new ArgumentException("Missing runtime command. Expected one of: agent, service, queue, approve, reject, trigger.");
        }

        var sub = args[1].ToLowerInvariant();
        return sub switch
        {
            "agent" => ParseRuntimeAgentCommand(global, args),
            "service" => ParseRuntimeServiceCommand(global, args),
            "queue" => ParseRuntimeQueueCommand(global, args),
            "approve" => ParseRuntimeApproveRejectCommand(global, args, approve: true),
            "reject" => ParseRuntimeApproveRejectCommand(global, args, approve: false),
            "trigger" => ParseRuntimeTriggerCommand(global, args),
            _ => throw new ArgumentException($"Unknown runtime command: {args[1]}")
        };
    }

    private static CliInvocation ParseRuntimeAgentCommand(GlobalOptions global, List<string> args)
    {
        if (args.Count < 3)
        {
            throw new ArgumentException("Missing runtime agent verb. Expected one of: start, install, uninstall.");
        }

        var verb = args[2].ToLowerInvariant();
        var tail = args.Skip(3).ToList();
        switch (verb)
        {
            case "start":
            {
                var options = new RuntimeAgentStartOptions(
                    RulesPath: ConsumeOptionalValue(tail, "--rules-path"),
                    PollSeconds: ConsumeOptionalInt(tail, "--poll-seconds", 0),
                    Once: ConsumeFlag(tail, "--once"));
                if (options.PollSeconds < 0)
                {
                    throw new ArgumentException("--poll-seconds must be >= 0");
                }

                return Build(global, CommandId.RuntimeAgentStart, options, tail);
            }
            case "install":
                return Build(global, CommandId.RuntimeAgentInstall, new RuntimeAgentInstallOptions(), tail);
            case "uninstall":
                return Build(global, CommandId.RuntimeAgentUninstall, new RuntimeAgentUninstallOptions(), tail);
            default:
                throw new ArgumentException($"Unknown runtime agent verb: {args[2]}");
        }
    }

    private static CliInvocation ParseRuntimeServiceCommand(GlobalOptions global, List<string> args)
    {
        if (args.Count < 3)
        {
            throw new ArgumentException("Missing runtime service verb. Expected one of: run, install, start, stop, uninstall, status.");
        }

        var verb = args[2].ToLowerInvariant();
        var tail = args.Skip(3).ToList();
        return verb switch
        {
            "run" => Build(global, CommandId.RuntimeServiceRun, new RuntimeServiceRunOptions(ConsumeOptionalValue(tail, "--rules-path")), tail),
            "install" => Build(global, CommandId.RuntimeServiceInstall, new RuntimeServiceInstallOptions(), tail),
            "start" => Build(global, CommandId.RuntimeServiceStart, new RuntimeServiceStartOptions(), tail),
            "stop" => Build(global, CommandId.RuntimeServiceStop, new RuntimeServiceStopOptions(), tail),
            "uninstall" => Build(global, CommandId.RuntimeServiceUninstall, new RuntimeServiceUninstallOptions(), tail),
            "status" => Build(global, CommandId.RuntimeServiceStatus, new RuntimeServiceStatusOptions(), tail),
            _ => throw new ArgumentException($"Unknown runtime service verb: {args[2]}")
        };
    }

    private static CliInvocation ParseRuntimeQueueCommand(GlobalOptions global, List<string> args)
    {
        if (args.Count < 3 || !args[2].Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Expected command 'runtime queue list'.");
        }

        var tail = args.Skip(3).ToList();
        return Build(global, CommandId.RuntimeQueueList, new RuntimeQueueListOptions(), tail);
    }

    private static CliInvocation ParseRuntimeApproveRejectCommand(GlobalOptions global, List<string> args, bool approve)
    {
        if (args.Count < 3)
        {
            throw new ArgumentException($"Missing action id for runtime {(approve ? "approve" : "reject")}.");
        }

        var actionId = args[2];
        if (string.IsNullOrWhiteSpace(actionId))
        {
            throw new ArgumentException($"Invalid action id for runtime {(approve ? "approve" : "reject")}.");
        }

        var tail = args.Skip(3).ToList();
        var note = ConsumeOptionalValue(tail, "--note");
        return approve
            ? Build(global, CommandId.RuntimeApprove, new RuntimeApproveOptions(actionId, note), tail)
            : Build(global, CommandId.RuntimeReject, new RuntimeRejectOptions(actionId, note), tail);
    }

    private static CliInvocation ParseRuntimeTriggerCommand(GlobalOptions global, List<string> args)
    {
        if (args.Count < 3)
        {
            throw new ArgumentException("Missing event name for runtime trigger.");
        }

        var eventName = args[2];
        if (string.IsNullOrWhiteSpace(eventName))
        {
            throw new ArgumentException("Invalid event name for runtime trigger.");
        }

        var tail = args.Skip(3).ToList();
        var options = new RuntimeTriggerOptions(
            EventName: eventName,
            PayloadJsonPath: ConsumeOptionalValue(tail, "--payload-json"));
        return Build(global, CommandId.RuntimeTrigger, options, tail);
    }

    private static CliInvocation Build(GlobalOptions global, CommandId id, ICommandOptions options, List<string> tail)
    {
        if (tail.Count > 0)
        {
            throw new ArgumentException($"Unknown argument(s): {string.Join(" ", tail)}");
        }

        return new CliInvocation(id, global, options);
    }

    private static string ReadRequiredValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {option}");
        }

        index++;
        return args[index];
    }

    private static bool ConsumeFlag(List<string> args, string flag)
    {
        var index = args.FindIndex(a => a.Equals(flag, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return false;
        }

        args.RemoveAt(index);
        return true;
    }

    private static string? ConsumeOptionalValue(List<string> args, string option)
    {
        var index = args.FindIndex(a => a.Equals(option, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return null;
        }

        if (index + 1 >= args.Count)
        {
            throw new ArgumentException($"Missing value for {option}");
        }

        var value = args[index + 1];
        args.RemoveAt(index + 1);
        args.RemoveAt(index);
        return value;
    }

    private static string ConsumeRequiredValue(List<string> args, string option)
    {
        var value = ConsumeOptionalValue(args, option);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Missing value for {option}");
        }

        return value;
    }

    private static int ConsumeOptionalInt(List<string> args, string option, int fallback)
    {
        var raw = ConsumeOptionalValue(args, option);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        if (!int.TryParse(raw, out var value))
        {
            throw new ArgumentException($"Invalid integer for {option}: {raw}");
        }

        return value;
    }

    private static IReadOnlyList<string> ConsumeMultiValueOption(List<string> args, string option)
    {
        var values = new List<string>();
        while (true)
        {
            var index = args.FindIndex(a => a.Equals(option, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                break;
            }

            if (index + 1 >= args.Count)
            {
                throw new ArgumentException($"Missing value for {option}");
            }

            values.Add(args[index + 1]);
            args.RemoveAt(index + 1);
            args.RemoveAt(index);
        }

        return values;
    }

    private static void ValidateNonNegative(int value, string option)
    {
        if (value < 0)
        {
            throw new ArgumentException($"{option} must be >= 0");
        }
    }

    private static void EnsureVerb(List<string> args, string expectedGroup, string? expectedSub = null)
    {
        if (!args[0].Equals(expectedGroup, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Expected command group '{expectedGroup}'.");
        }

        if (expectedSub is null)
        {
            return;
        }

        if (args.Count < 2 || !args[1].Equals(expectedSub, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Expected command '{expectedGroup} {expectedSub}'.");
        }
    }
}

using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Application.Interfaces;

internal interface IRuntimeBridgeService
{
    CommandResult RuntimeAgentStart(CommandContext context, RuntimeAgentStartOptions options);
    CommandResult RuntimeAgentInstall(CommandContext context, RuntimeAgentInstallOptions options);
    CommandResult RuntimeAgentUninstall(CommandContext context, RuntimeAgentUninstallOptions options);
    CommandResult RuntimeServiceRun(CommandContext context, RuntimeServiceRunOptions options);
    CommandResult RuntimeServiceInstall(CommandContext context, RuntimeServiceInstallOptions options);
    CommandResult RuntimeServiceStart(CommandContext context, RuntimeServiceStartOptions options);
    CommandResult RuntimeServiceStop(CommandContext context, RuntimeServiceStopOptions options);
    CommandResult RuntimeServiceUninstall(CommandContext context, RuntimeServiceUninstallOptions options);
    CommandResult RuntimeServiceStatus(CommandContext context, RuntimeServiceStatusOptions options);
    CommandResult RuntimeQueueList(CommandContext context, RuntimeQueueListOptions options);
    CommandResult RuntimeApprove(CommandContext context, RuntimeApproveOptions options);
    CommandResult RuntimeReject(CommandContext context, RuntimeRejectOptions options);
    CommandResult RuntimeTrigger(CommandContext context, RuntimeTriggerOptions options);
}

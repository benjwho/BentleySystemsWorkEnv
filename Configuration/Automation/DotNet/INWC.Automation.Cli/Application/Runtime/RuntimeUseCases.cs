using INWC.Automation.Cli.Application.Interfaces;
using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Application.Runtime;

internal sealed class RuntimeAgentStartUseCase : ICommandUseCase<RuntimeAgentStartOptions, CommandResult>
{
    private readonly IRuntimeBridgeService _runtimeBridgeService;

    public RuntimeAgentStartUseCase(IRuntimeBridgeService runtimeBridgeService)
    {
        _runtimeBridgeService = runtimeBridgeService;
    }

    public CommandResult Execute(CommandContext context, RuntimeAgentStartOptions options)
    {
        return _runtimeBridgeService.RuntimeAgentStart(context, options);
    }
}

internal sealed class RuntimeAgentInstallUseCase : ICommandUseCase<RuntimeAgentInstallOptions, CommandResult>
{
    private readonly IRuntimeBridgeService _runtimeBridgeService;

    public RuntimeAgentInstallUseCase(IRuntimeBridgeService runtimeBridgeService)
    {
        _runtimeBridgeService = runtimeBridgeService;
    }

    public CommandResult Execute(CommandContext context, RuntimeAgentInstallOptions options)
    {
        return _runtimeBridgeService.RuntimeAgentInstall(context, options);
    }
}

internal sealed class RuntimeAgentUninstallUseCase : ICommandUseCase<RuntimeAgentUninstallOptions, CommandResult>
{
    private readonly IRuntimeBridgeService _runtimeBridgeService;

    public RuntimeAgentUninstallUseCase(IRuntimeBridgeService runtimeBridgeService)
    {
        _runtimeBridgeService = runtimeBridgeService;
    }

    public CommandResult Execute(CommandContext context, RuntimeAgentUninstallOptions options)
    {
        return _runtimeBridgeService.RuntimeAgentUninstall(context, options);
    }
}

internal sealed class RuntimeServiceRunUseCase : ICommandUseCase<RuntimeServiceRunOptions, CommandResult>
{
    private readonly IRuntimeBridgeService _runtimeBridgeService;

    public RuntimeServiceRunUseCase(IRuntimeBridgeService runtimeBridgeService)
    {
        _runtimeBridgeService = runtimeBridgeService;
    }

    public CommandResult Execute(CommandContext context, RuntimeServiceRunOptions options)
    {
        return _runtimeBridgeService.RuntimeServiceRun(context, options);
    }
}

internal sealed class RuntimeServiceInstallUseCase : ICommandUseCase<RuntimeServiceInstallOptions, CommandResult>
{
    private readonly IRuntimeBridgeService _runtimeBridgeService;

    public RuntimeServiceInstallUseCase(IRuntimeBridgeService runtimeBridgeService)
    {
        _runtimeBridgeService = runtimeBridgeService;
    }

    public CommandResult Execute(CommandContext context, RuntimeServiceInstallOptions options)
    {
        return _runtimeBridgeService.RuntimeServiceInstall(context, options);
    }
}

internal sealed class RuntimeServiceStartUseCase : ICommandUseCase<RuntimeServiceStartOptions, CommandResult>
{
    private readonly IRuntimeBridgeService _runtimeBridgeService;

    public RuntimeServiceStartUseCase(IRuntimeBridgeService runtimeBridgeService)
    {
        _runtimeBridgeService = runtimeBridgeService;
    }

    public CommandResult Execute(CommandContext context, RuntimeServiceStartOptions options)
    {
        return _runtimeBridgeService.RuntimeServiceStart(context, options);
    }
}

internal sealed class RuntimeServiceStopUseCase : ICommandUseCase<RuntimeServiceStopOptions, CommandResult>
{
    private readonly IRuntimeBridgeService _runtimeBridgeService;

    public RuntimeServiceStopUseCase(IRuntimeBridgeService runtimeBridgeService)
    {
        _runtimeBridgeService = runtimeBridgeService;
    }

    public CommandResult Execute(CommandContext context, RuntimeServiceStopOptions options)
    {
        return _runtimeBridgeService.RuntimeServiceStop(context, options);
    }
}

internal sealed class RuntimeServiceUninstallUseCase : ICommandUseCase<RuntimeServiceUninstallOptions, CommandResult>
{
    private readonly IRuntimeBridgeService _runtimeBridgeService;

    public RuntimeServiceUninstallUseCase(IRuntimeBridgeService runtimeBridgeService)
    {
        _runtimeBridgeService = runtimeBridgeService;
    }

    public CommandResult Execute(CommandContext context, RuntimeServiceUninstallOptions options)
    {
        return _runtimeBridgeService.RuntimeServiceUninstall(context, options);
    }
}

internal sealed class RuntimeServiceStatusUseCase : ICommandUseCase<RuntimeServiceStatusOptions, CommandResult>
{
    private readonly IRuntimeBridgeService _runtimeBridgeService;

    public RuntimeServiceStatusUseCase(IRuntimeBridgeService runtimeBridgeService)
    {
        _runtimeBridgeService = runtimeBridgeService;
    }

    public CommandResult Execute(CommandContext context, RuntimeServiceStatusOptions options)
    {
        return _runtimeBridgeService.RuntimeServiceStatus(context, options);
    }
}

internal sealed class RuntimeQueueListUseCase : ICommandUseCase<RuntimeQueueListOptions, CommandResult>
{
    private readonly IRuntimeBridgeService _runtimeBridgeService;

    public RuntimeQueueListUseCase(IRuntimeBridgeService runtimeBridgeService)
    {
        _runtimeBridgeService = runtimeBridgeService;
    }

    public CommandResult Execute(CommandContext context, RuntimeQueueListOptions options)
    {
        return _runtimeBridgeService.RuntimeQueueList(context, options);
    }
}

internal sealed class RuntimeApproveUseCase : ICommandUseCase<RuntimeApproveOptions, CommandResult>
{
    private readonly IRuntimeBridgeService _runtimeBridgeService;

    public RuntimeApproveUseCase(IRuntimeBridgeService runtimeBridgeService)
    {
        _runtimeBridgeService = runtimeBridgeService;
    }

    public CommandResult Execute(CommandContext context, RuntimeApproveOptions options)
    {
        return _runtimeBridgeService.RuntimeApprove(context, options);
    }
}

internal sealed class RuntimeRejectUseCase : ICommandUseCase<RuntimeRejectOptions, CommandResult>
{
    private readonly IRuntimeBridgeService _runtimeBridgeService;

    public RuntimeRejectUseCase(IRuntimeBridgeService runtimeBridgeService)
    {
        _runtimeBridgeService = runtimeBridgeService;
    }

    public CommandResult Execute(CommandContext context, RuntimeRejectOptions options)
    {
        return _runtimeBridgeService.RuntimeReject(context, options);
    }
}

internal sealed class RuntimeTriggerUseCase : ICommandUseCase<RuntimeTriggerOptions, CommandResult>
{
    private readonly IRuntimeBridgeService _runtimeBridgeService;

    public RuntimeTriggerUseCase(IRuntimeBridgeService runtimeBridgeService)
    {
        _runtimeBridgeService = runtimeBridgeService;
    }

    public CommandResult Execute(CommandContext context, RuntimeTriggerOptions options)
    {
        return _runtimeBridgeService.RuntimeTrigger(context, options);
    }
}

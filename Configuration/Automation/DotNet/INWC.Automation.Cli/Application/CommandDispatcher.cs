using INWC.Automation.Cli.Application.Interfaces;
using INWC.Automation.Cli.Application.Runtime;
using INWC.Automation.Cli.Application.UseCases;
using INWC.Automation.Cli.Compatibility;
using INWC.Automation.Cli.Domain.Models;
using INWC.Automation.Cli.Infrastructure.Audit;
using INWC.Automation.Cli.Infrastructure.Config;
using INWC.Automation.Cli.Infrastructure.Processes;
using INWC.Automation.Cli.Infrastructure.Runtime;
using INWC.Automation.Cli.Infrastructure.System;

namespace INWC.Automation.Cli.Application;

internal sealed class CommandDispatcher
{
    private readonly CheckEnvUseCase _checkEnv;
    private readonly CheckIntegrationUseCase _checkIntegration;
    private readonly CheckFullHealthUseCase _checkFullHealth;
    private readonly FixPickerRootsUseCase _fixPickerRoots;
    private readonly AuditPickerUseCase _auditPicker;
    private readonly SmokePythonUseCase _smokePython;
    private readonly SmokeProjectWiseUseCase _smokeProjectWise;
    private readonly DetectIntegrationsUseCase _detectIntegrations;
    private readonly RepairProgramDataUseCase _repairProgramData;
    private readonly BackupCreateUseCase _backupCreate;
    private readonly BackupRestoreUseCase _backupRestore;
    private readonly ResetRebuildUseCase _resetRebuild;
    private readonly InitWorkspaceReportUseCase _initWorkspaceReport;
    private readonly RuntimeAgentStartUseCase _runtimeAgentStart;
    private readonly RuntimeAgentInstallUseCase _runtimeAgentInstall;
    private readonly RuntimeAgentUninstallUseCase _runtimeAgentUninstall;
    private readonly RuntimeServiceRunUseCase _runtimeServiceRun;
    private readonly RuntimeServiceInstallUseCase _runtimeServiceInstall;
    private readonly RuntimeServiceStartUseCase _runtimeServiceStart;
    private readonly RuntimeServiceStopUseCase _runtimeServiceStop;
    private readonly RuntimeServiceUninstallUseCase _runtimeServiceUninstall;
    private readonly RuntimeServiceStatusUseCase _runtimeServiceStatus;
    private readonly RuntimeQueueListUseCase _runtimeQueueList;
    private readonly RuntimeApproveUseCase _runtimeApprove;
    private readonly RuntimeRejectUseCase _runtimeReject;
    private readonly RuntimeTriggerUseCase _runtimeTrigger;

    public CommandDispatcher()
    {
        var configReader = new ConfigFileReader();
        var artifactPathPolicy = new ArtifactPathPolicy();
        var exitCodePolicy = new ExitCodePolicy();
        var processRunner = new ProcessRunner();
        var executableResolver = new ExecutableResolver();

        var legacyBridge = new LegacyScriptBridge(processRunner, exitCodePolicy);
        IRepairService repairService = new LegacyRepairService(legacyBridge);
        IBackupRestoreService backupRestoreService = new LegacyBackupRestoreService(legacyBridge);
        IResetRebuildService resetRebuildService = new LegacyResetRebuildService(legacyBridge);
        IPickerAuditService pickerAuditService = new PickerAuditService(artifactPathPolicy, executableResolver, configReader);
        IHealthCheckRunner healthCheckRunner = new HealthCheckRunner(processRunner);
        IRuntimeBridgeService runtimeBridgeService = new RuntimeBridgeService(configReader, processRunner);

        _checkEnv = new CheckEnvUseCase(configReader, exitCodePolicy);
        _checkIntegration = new CheckIntegrationUseCase(configReader, exitCodePolicy);
        _checkFullHealth = new CheckFullHealthUseCase(healthCheckRunner, artifactPathPolicy, exitCodePolicy);
        _fixPickerRoots = new FixPickerRootsUseCase(configReader, exitCodePolicy);
        _auditPicker = new AuditPickerUseCase(pickerAuditService);
        _smokePython = new SmokePythonUseCase(configReader, artifactPathPolicy, exitCodePolicy, processRunner);
        _smokeProjectWise = new SmokeProjectWiseUseCase(configReader, artifactPathPolicy, exitCodePolicy);
        _detectIntegrations = new DetectIntegrationsUseCase(artifactPathPolicy, exitCodePolicy, configReader);
        _repairProgramData = new RepairProgramDataUseCase(repairService);
        _backupCreate = new BackupCreateUseCase(backupRestoreService);
        _backupRestore = new BackupRestoreUseCase(backupRestoreService);
        _resetRebuild = new ResetRebuildUseCase(resetRebuildService);
        _initWorkspaceReport = new InitWorkspaceReportUseCase(exitCodePolicy);
        _runtimeAgentStart = new RuntimeAgentStartUseCase(runtimeBridgeService);
        _runtimeAgentInstall = new RuntimeAgentInstallUseCase(runtimeBridgeService);
        _runtimeAgentUninstall = new RuntimeAgentUninstallUseCase(runtimeBridgeService);
        _runtimeServiceRun = new RuntimeServiceRunUseCase(runtimeBridgeService);
        _runtimeServiceInstall = new RuntimeServiceInstallUseCase(runtimeBridgeService);
        _runtimeServiceStart = new RuntimeServiceStartUseCase(runtimeBridgeService);
        _runtimeServiceStop = new RuntimeServiceStopUseCase(runtimeBridgeService);
        _runtimeServiceUninstall = new RuntimeServiceUninstallUseCase(runtimeBridgeService);
        _runtimeServiceStatus = new RuntimeServiceStatusUseCase(runtimeBridgeService);
        _runtimeQueueList = new RuntimeQueueListUseCase(runtimeBridgeService);
        _runtimeApprove = new RuntimeApproveUseCase(runtimeBridgeService);
        _runtimeReject = new RuntimeRejectUseCase(runtimeBridgeService);
        _runtimeTrigger = new RuntimeTriggerUseCase(runtimeBridgeService);
    }

    public CommandResult Execute(CliInvocation invocation)
    {
        var context = new CommandContext(invocation.Global);

        return invocation.CommandId switch
        {
            CommandId.CheckEnv => _checkEnv.Execute(context, (CheckEnvOptions)invocation.Options),
            CommandId.CheckIntegration => _checkIntegration.Execute(context, (CheckIntegrationOptions)invocation.Options),
            CommandId.CheckFullHealth => _checkFullHealth.Execute(context, (CheckFullHealthOptions)invocation.Options),
            CommandId.FixPickerRoots => _fixPickerRoots.Execute(context, (FixPickerRootsOptions)invocation.Options),
            CommandId.AuditPicker => _auditPicker.Execute(context, (AuditPickerOptions)invocation.Options),
            CommandId.SmokePython => _smokePython.Execute(context, (SmokePythonOptions)invocation.Options),
            CommandId.SmokeProjectWise => _smokeProjectWise.Execute(context, (SmokeProjectWiseOptions)invocation.Options),
            CommandId.DetectIntegrations => _detectIntegrations.Execute(context, (DetectIntegrationsOptions)invocation.Options),
            CommandId.RepairProgramData => _repairProgramData.Execute(context, (RepairProgramDataOptions)invocation.Options),
            CommandId.BackupCreate => _backupCreate.Execute(context, (BackupCreateOptions)invocation.Options),
            CommandId.BackupRestore => _backupRestore.Execute(context, (BackupRestoreOptions)invocation.Options),
            CommandId.ResetRebuild => _resetRebuild.Execute(context, (ResetRebuildOptions)invocation.Options),
            CommandId.InitWorkspaceReport => _initWorkspaceReport.Execute(context, (InitWorkspaceReportOptions)invocation.Options),
            CommandId.RuntimeAgentStart => _runtimeAgentStart.Execute(context, (RuntimeAgentStartOptions)invocation.Options),
            CommandId.RuntimeAgentInstall => _runtimeAgentInstall.Execute(context, (RuntimeAgentInstallOptions)invocation.Options),
            CommandId.RuntimeAgentUninstall => _runtimeAgentUninstall.Execute(context, (RuntimeAgentUninstallOptions)invocation.Options),
            CommandId.RuntimeServiceRun => _runtimeServiceRun.Execute(context, (RuntimeServiceRunOptions)invocation.Options),
            CommandId.RuntimeServiceInstall => _runtimeServiceInstall.Execute(context, (RuntimeServiceInstallOptions)invocation.Options),
            CommandId.RuntimeServiceStart => _runtimeServiceStart.Execute(context, (RuntimeServiceStartOptions)invocation.Options),
            CommandId.RuntimeServiceStop => _runtimeServiceStop.Execute(context, (RuntimeServiceStopOptions)invocation.Options),
            CommandId.RuntimeServiceUninstall => _runtimeServiceUninstall.Execute(context, (RuntimeServiceUninstallOptions)invocation.Options),
            CommandId.RuntimeServiceStatus => _runtimeServiceStatus.Execute(context, (RuntimeServiceStatusOptions)invocation.Options),
            CommandId.RuntimeQueueList => _runtimeQueueList.Execute(context, (RuntimeQueueListOptions)invocation.Options),
            CommandId.RuntimeApprove => _runtimeApprove.Execute(context, (RuntimeApproveOptions)invocation.Options),
            CommandId.RuntimeReject => _runtimeReject.Execute(context, (RuntimeRejectOptions)invocation.Options),
            CommandId.RuntimeTrigger => _runtimeTrigger.Execute(context, (RuntimeTriggerOptions)invocation.Options),
            _ => CommandResult.Failure($"Unsupported command id: {invocation.CommandId}")
        };
    }
}

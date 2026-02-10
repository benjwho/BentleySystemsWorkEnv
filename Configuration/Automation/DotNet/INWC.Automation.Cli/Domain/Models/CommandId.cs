namespace INWC.Automation.Cli.Domain.Models;

internal enum CommandId
{
    CheckEnv,
    CheckIntegration,
    CheckFullHealth,
    FixPickerRoots,
    AuditPicker,
    SmokePython,
    SmokeProjectWise,
    DetectIntegrations,
    RepairProgramData,
    BackupCreate,
    BackupRestore,
    ResetRebuild,
    InitWorkspaceReport,
    RuntimeAgentStart,
    RuntimeAgentInstall,
    RuntimeAgentUninstall,
    RuntimeServiceRun,
    RuntimeServiceInstall,
    RuntimeServiceStart,
    RuntimeServiceStop,
    RuntimeServiceUninstall,
    RuntimeServiceStatus,
    RuntimeQueueList,
    RuntimeApprove,
    RuntimeReject,
    RuntimeTrigger
}

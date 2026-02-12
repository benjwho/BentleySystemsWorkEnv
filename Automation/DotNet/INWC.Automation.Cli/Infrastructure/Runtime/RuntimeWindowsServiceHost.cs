using System.ServiceProcess;
using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Infrastructure.Runtime;

internal sealed class RuntimeWindowsServiceHost : ServiceBase
{
    private readonly RuntimeServiceCoordinator _coordinator;
    private readonly CommandContext _context;
    private readonly string? _rulesPath;
    private CancellationTokenSource? _cts;
    private Task? _runTask;

    public RuntimeWindowsServiceHost(RuntimeServiceCoordinator coordinator, CommandContext context, string? rulesPath)
    {
        _coordinator = coordinator;
        _context = context;
        _rulesPath = rulesPath;
        ServiceName = RuntimeConstants.ServiceName;
    }

    protected override void OnStart(string[] args)
    {
        _cts = new CancellationTokenSource();
        _runTask = Task.Run(() => _coordinator.Run(_context, _rulesPath, _cts.Token));
    }

    protected override void OnStop()
    {
        _cts?.Cancel();
        try
        {
            _runTask?.Wait(TimeSpan.FromSeconds(20));
        }
        catch
        {
            // ignored on purpose
        }
    }
}

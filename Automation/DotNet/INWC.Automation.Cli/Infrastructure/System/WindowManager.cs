using System.Diagnostics;
using System.Runtime.InteropServices;

namespace INWC.Automation.Cli.Infrastructure.System;

internal interface IWindowManager
{
    bool Focus(Process process, int timeoutSeconds);
    IntPtr GetMainWindowHandle(Process process);
    bool IsForegroundWindowOwnedBy(Process process);
    void Close(Process process, Action<string> warn);
}

internal sealed class WindowManager : IWindowManager
{
    public bool Focus(Process process, int timeoutSeconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(Math.Max(1, timeoutSeconds));
        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                return false;
            }

            process.Refresh();
            var handle = process.MainWindowHandle;
            if (handle != IntPtr.Zero)
            {
                NativeMethods.ShowWindowAsync(handle, 9);
                NativeMethods.SetForegroundWindow(handle);
                Thread.Sleep(300);
                return true;
            }

            Thread.Sleep(300);
        }

        return false;
    }

    public IntPtr GetMainWindowHandle(Process process)
    {
        if (process.HasExited)
        {
            return IntPtr.Zero;
        }

        process.Refresh();
        return process.MainWindowHandle;
    }

    public bool IsForegroundWindowOwnedBy(Process process)
    {
        if (process.HasExited)
        {
            return false;
        }

        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            return false;
        }

        NativeMethods.GetWindowThreadProcessId(foreground, out var ownerPid);
        return ownerPid == process.Id;
    }

    public void Close(Process process, Action<string> warn)
    {
        if (process.HasExited)
        {
            return;
        }

        try
        {
            process.CloseMainWindow();
            if (!process.WaitForExit(5000))
            {
                process.Kill(true);
            }
        }
        catch (Exception ex)
        {
            warn($"Unable to close process {process.ProcessName} ({process.Id}) cleanly: {ex.Message}");
        }
    }
}

internal static class NativeMethods
{
    [DllImport("user32.dll")]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    internal static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}

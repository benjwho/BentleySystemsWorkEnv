using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace INWC.Automation.Cli.Infrastructure.System;

internal interface IScreenshotService
{
    bool CaptureWindow(IntPtr windowHandle, string outputPath);
    void CaptureDesktop(string outputPath);
}

internal sealed class ScreenshotService : IScreenshotService
{
    public bool CaptureWindow(IntPtr windowHandle, string outputPath)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return false;
        }

        if (!NativeScreenshot.GetWindowRect(windowHandle, out var rect))
        {
            return false;
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);

        var printed = false;
        var hdc = graphics.GetHdc();
        try
        {
            printed = NativeScreenshot.PrintWindow(windowHandle, hdc, 0);
        }
        finally
        {
            graphics.ReleaseHdc(hdc);
        }

        if (!printed)
        {
            graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height));
        }

        bitmap.Save(outputPath, ImageFormat.Png);
        return true;
    }

    public void CaptureDesktop(string outputPath)
    {
        var bounds = SystemInformation.VirtualScreen;
        using var bitmap = new Bitmap(bounds.Width, bounds.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
        bitmap.Save(outputPath, ImageFormat.Png);
    }
}

internal static class NativeScreenshot
{
    [DllImport("user32.dll")]
    internal static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("user32.dll")]
    internal static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);
}

[StructLayout(LayoutKind.Sequential)]
internal struct Rect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AppVolumeHotkeys;

internal static class ForegroundProcessService
{
    public static string GetForegroundProcessName()
    {
        var handle = GetForegroundWindow();
        if (handle == IntPtr.Zero)
        {
            return string.Empty;
        }

        GetWindowThreadProcessId(handle, out var processId);
        if (processId == 0)
        {
            return string.Empty;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? process.ProcessName
                : process.ProcessName + ".exe";
        }
        catch
        {
            return string.Empty;
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}

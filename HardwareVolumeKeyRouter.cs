using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AppVolumeHotkeys;

internal sealed class HardwareVolumeKeyRouter : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int VkVolumeMute = 0xAD;
    private const int VkVolumeDown = 0xAE;
    private const int VkVolumeUp = 0xAF;

    private readonly LowLevelKeyboardProc _callback;
    private IntPtr _hookHandle;
    private bool _isDisposed;

    public bool Enabled { get; set; }
    public bool LogKeyboardEvents { get; set; }

    public event EventHandler<HardwareVolumeKeyEventArgs>? VolumeKeyPressed;

    public HardwareVolumeKeyRouter()
    {
        _callback = HookCallback;
    }

    public void Start()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            return;
        }

        using var currentProcess = Process.GetCurrentProcess();
        var currentModule = currentProcess.MainModule;
        var moduleHandle = currentModule is null ? IntPtr.Zero : GetModuleHandle(currentModule.ModuleName);
        _hookHandle = SetWindowsHookEx(WhKeyboardLl, _callback, moduleHandle, 0);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        if (_hookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        var message = wParam.ToInt32();
        var data = nCode >= 0
            ? Marshal.PtrToStructure<KbdLlHookStruct>(lParam)
            : default;

        if (LogKeyboardEvents && nCode >= 0)
        {
            LogKeyboardEvent(message, data);
        }

        if (Enabled && nCode >= 0 && (message == WmKeyDown || message == WmSysKeyDown))
        {
            var action = data.VkCode switch
            {
                VkVolumeUp => HardwareVolumeAction.Up,
                VkVolumeDown => HardwareVolumeAction.Down,
                VkVolumeMute => HardwareVolumeAction.ToggleMute,
                _ => HardwareVolumeAction.None
            };

            if (action != HardwareVolumeAction.None)
            {
                VolumeKeyPressed?.Invoke(this, new HardwareVolumeKeyEventArgs(action));
                return new IntPtr(1);
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private static void LogKeyboardEvent(int message, KbdLlHookStruct data)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.DataDirectory);
            var keyName = Enum.IsDefined(typeof(Keys), data.VkCode)
                ? ((Keys)data.VkCode).ToString()
                : "Unknown";
            var line = string.Join(
                " | ",
                DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"),
                $"message={GetMessageName(message)}",
                $"vk=0x{data.VkCode:X2}",
                $"key={keyName}",
                $"scan=0x{data.ScanCode:X2}",
                $"flags=0x{data.Flags:X8}",
                $"time={data.Time}") + Environment.NewLine;
            File.AppendAllText(AppPaths.KeyboardLogPath, line);
        }
        catch
        {
            // Diagnostics must never break the hook chain.
        }
    }

    private static string GetMessageName(int message)
    {
        return message switch
        {
            0x0100 => "WM_KEYDOWN",
            0x0101 => "WM_KEYUP",
            0x0104 => "WM_SYSKEYDOWN",
            0x0105 => "WM_SYSKEYUP",
            _ => $"0x{message:X4}"
        };
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        public int VkCode;
        public int ScanCode;
        public int Flags;
        public int Time;
        public IntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}

internal enum HardwareVolumeAction
{
    None,
    Up,
    Down,
    ToggleMute
}

internal sealed class HardwareVolumeKeyEventArgs(HardwareVolumeAction action) : EventArgs
{
    public HardwareVolumeAction Action { get; } = action;
}

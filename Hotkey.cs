using System.Runtime.InteropServices;

namespace AppVolumeHotkeys;

[Flags]
internal enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008
}

internal sealed record HotkeyDefinition(Keys Key, HotkeyModifiers Modifiers)
{
    public bool IsValid => Key != Keys.None && Modifiers != HotkeyModifiers.None;

    public override string ToString()
    {
        if (!IsValid)
        {
            return Localizer.T("HotkeyNone");
        }

        var parts = new List<string>();
        if (Modifiers.HasFlag(HotkeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Win))
        {
            parts.Add("Win");
        }

        parts.Add(Key.ToString());
        return string.Join(" + ", parts);
    }

    public static HotkeyDefinition FromKeyEvent(KeyEventArgs e)
    {
        var modifiers = HotkeyModifiers.None;
        if (e.Control)
        {
            modifiers |= HotkeyModifiers.Control;
        }

        if (e.Alt)
        {
            modifiers |= HotkeyModifiers.Alt;
        }

        if (e.Shift)
        {
            modifiers |= HotkeyModifiers.Shift;
        }

        if ((e.Modifiers & Keys.LWin) == Keys.LWin || (e.Modifiers & Keys.RWin) == Keys.RWin)
        {
            modifiers |= HotkeyModifiers.Win;
        }

        var key = e.KeyCode is Keys.ControlKey or Keys.Menu or Keys.ShiftKey or Keys.LWin or Keys.RWin
            ? Keys.None
            : e.KeyCode;

        return new HotkeyDefinition(key, modifiers);
    }
}

internal sealed class HotkeyTextBox : TextBox
{
    public HotkeyDefinition Value { get; private set; } = new(Keys.None, HotkeyModifiers.None);

    public event EventHandler? ValueChangedByUser;

    public HotkeyTextBox()
    {
        ReadOnly = true;
        ShortcutsEnabled = false;
        TabStop = true;
    }

    public void SetValue(HotkeyDefinition value)
    {
        Value = value;
        Text = value.ToString();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var e = new KeyEventArgs(keyData);
        CaptureHotkey(e);
        return true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        CaptureHotkey(e);
        base.OnKeyDown(e);
    }

    private void CaptureHotkey(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Delete or Keys.Back)
        {
            SetValue(new HotkeyDefinition(Keys.None, HotkeyModifiers.None));
            ValueChangedByUser?.Invoke(this, EventArgs.Empty);
            e.SuppressKeyPress = true;
            return;
        }

        var value = HotkeyDefinition.FromKeyEvent(e);
        if (!value.IsValid)
        {
            Text = Localizer.T("HotkeyPrompt");
            return;
        }

        SetValue(value);
        ValueChangedByUser?.Invoke(this, EventArgs.Empty);
        e.SuppressKeyPress = true;
    }
}

internal static class GlobalHotkeys
{
    public const int WmHotkey = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, HotkeyModifiers fsModifiers, Keys vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

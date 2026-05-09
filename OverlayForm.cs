using System.Runtime.InteropServices;

namespace AppVolumeHotkeys;

internal sealed class OverlayForm : Form
{
    private static readonly IntPtr HwndTopmost = new(-1);
    private static readonly IntPtr HwndNoTopmost = new(-2);
    private const int WsExTopmost = 0x00000008;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExLayered = 0x00080000;
    private const int WsExNoActivate = 0x08000000;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpShowWindow = 0x0040;

    private readonly Label _titleLabel = new();
    private readonly Label _detailLabel = new();
    private readonly System.Windows.Forms.Timer _hideTimer = new();
    private readonly System.Windows.Forms.Timer _pinTopMostTimer = new();

    public OverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(380, 112);
        BackColor = Color.FromArgb(24, 28, 32);
        Opacity = 0.94;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(18, 14, 18, 14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        Controls.Add(root);

        _titleLabel.Dock = DockStyle.Fill;
        _titleLabel.ForeColor = Color.White;
        _titleLabel.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        _titleLabel.AutoEllipsis = true;
        root.Controls.Add(_titleLabel, 0, 0);

        _detailLabel.Dock = DockStyle.Fill;
        _detailLabel.ForeColor = Color.FromArgb(195, 232, 220);
        _detailLabel.Font = new Font("Segoe UI", 10.5f, FontStyle.Regular);
        _detailLabel.TextAlign = ContentAlignment.MiddleLeft;
        _detailLabel.AutoEllipsis = true;
        root.Controls.Add(_detailLabel, 0, 1);

        _hideTimer.Interval = 5000;
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            _pinTopMostTimer.Stop();
            Hide();
        };

        _pinTopMostTimer.Interval = 250;
        _pinTopMostTimer.Tick += (_, _) => PinTopMost();
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= WsExTopmost | WsExToolWindow | WsExLayered | WsExNoActivate;
            return parameters;
        }
    }

    public void ShowAction(string processName, string action, int volumePercent, bool isMuted)
    {
        if (IsHandleCreated && !Visible)
        {
            RecreateHandle();
        }

        _titleLabel.Text = processName;
        _detailLabel.Text = isMuted
            ? $"{action}: mute, {volumePercent}%"
            : $"{action}: {volumePercent}%";

        var area = Screen.FromPoint(Cursor.Position)?.Bounds ?? Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1280, 720);
        Location = new Point(area.Right - Width - 24, area.Bottom - Height - 24);

        _hideTimer.Stop();
        Show();
        RestoreTopMostState();
        PinTopMost();
        _hideTimer.Start();
        _pinTopMostTimer.Start();
    }

    private void PinTopMost()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        SetWindowPos(Handle, HwndTopmost, Left, Top, Width, Height, SwpNoActivate | SwpShowWindow);
    }

    private void RestoreTopMostState()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        TopMost = false;
        TopMost = true;
        SetWindowPos(Handle, HwndNoTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);
        SetWindowPos(Handle, HwndTopmost, Left, Top, Width, Height, SwpNoActivate | SwpShowWindow);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);
}

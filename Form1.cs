using System.ComponentModel;
using System.Runtime.InteropServices;

namespace AppVolumeHotkeys;

public partial class Form1 : Form
{
    private const int HotkeyVolumeUp = 1;
    private const int HotkeyVolumeDown = 2;
    private const int HotkeyToggleMute = 3;

    private enum VolumeAction
    {
        Up,
        Down,
        ToggleMute
    }

    private sealed record ActionResultSummary(int ChangedCount, int TargetCount, int AverageVolumePercent, bool AllMuted, string Title);

    private readonly AudioSessionService _audioSessionService = new();
    private readonly AppSettings _settings;
    private readonly BindingList<AudioSessionInfo> _sessions = [];
    private readonly DataGridView _sessionsGrid = new();
    private readonly CheckedListBox _targetAppsList = new();
    private readonly ComboBox _targetProcessCombo = new();
    private readonly ComboBox _languageCombo = new();
    private readonly NumericUpDown _stepInput = new();
    private readonly HotkeyTextBox _volumeUpHotkey = new();
    private readonly HotkeyTextBox _volumeDownHotkey = new();
    private readonly HotkeyTextBox _toggleMuteHotkey = new();
    private readonly CheckBox _startWithWindowsCheckBox = new();
    private readonly CheckBox _routeHardwareVolumeCheckBox = new();
    private readonly CheckBox _logKeyboardCheckBox = new();
    private readonly Label _statusLabel = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new();
    private readonly Icon _appIcon = AppIconFactory.CreateIcon();
    private readonly OverlayForm _overlayForm = new();
    private readonly HardwareVolumeKeyRouter _hardwareVolumeKeyRouter = new();
    private readonly bool _startMinimized;
    private ContextMenuStrip? _trayMenu;
    private NotifyIcon? _notifyIcon;
    private bool _initialVisibilityHandled;
    private bool _isLoadingUi;
    private bool _isRefreshingSessions;
    private bool _isExiting;

    public Form1(bool startMinimized)
    {
        InitializeComponent();
        _startMinimized = startMinimized;
        _settings = AppSettingsStore.Load();
        if (string.IsNullOrWhiteSpace(_settings.Language))
        {
            _settings.Language = "system";
        }

        Localizer.SetLanguage(_settings.Language);

        Text = "App Volume Hotkeys";
        Icon = _appIcon;
        MinimumSize = new Size(900, 620);
        Size = new Size(980, 680);
        StartPosition = FormStartPosition.CenterScreen;

        MigrateLegacySettings();
        BuildUi();
        BuildTrayIcon();
        LoadSettingsIntoUi();
        ApplyLocalization();
        RefreshSessions();
        RegisterAllHotkeys();
        StartHardwareVolumeRouter();

        _refreshTimer.Interval = 5000;
        _refreshTimer.Tick += (_, _) => RefreshSessions(keepStatus: true);
        _refreshTimer.Start();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == GlobalHotkeys.WmHotkey)
        {
            HandleHotkey(m.WParam.ToInt32());
            return;
        }

        base.WndProc(ref m);
    }

    protected override void SetVisibleCore(bool value)
    {
        if (!_initialVisibilityHandled)
        {
            _initialVisibilityHandled = true;
            if (_startMinimized)
            {
                ShowInTaskbar = false;
                base.SetVisibleCore(false);
                return;
            }
        }

        base.SetVisibleCore(value);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_isExiting && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            ShowInTaskbar = false;
            Hide();
            RegisterAllHotkeys();
            _notifyIcon?.ShowBalloonTip(1500, "App Volume Hotkeys", Localizer.T("TrayMessage"), ToolTipIcon.Info);
            return;
        }

        SaveSettingsFromUi(registerHotkeys: true);
        UnregisterAllHotkeys();
        _hardwareVolumeKeyRouter.Dispose();
        _notifyIcon?.Dispose();
        _overlayForm.Dispose();
        _appIcon.Dispose();
        base.OnFormClosing(e);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 204));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        Controls.Add(root);

        var optionsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 5
        };
        optionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        optionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        optionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        optionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        for (var i = 0; i < 5; i++)
        {
            optionsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        }

        root.Controls.Add(optionsPanel, 0, 0);

        optionsPanel.Controls.Add(NewLabel("AddProcess"), 0, 0);
        _targetProcessCombo.Dock = DockStyle.Fill;
        _targetProcessCombo.DropDownStyle = ComboBoxStyle.DropDown;
        optionsPanel.Controls.Add(_targetProcessCombo, 1, 0);

        optionsPanel.Controls.Add(NewLabel("VolumeStep"), 2, 0);
        _stepInput.Dock = DockStyle.Fill;
        _stepInput.Minimum = 1;
        _stepInput.Maximum = 50;
        _stepInput.ValueChanged += (_, _) => SaveSettingsFromUi(registerHotkeys: false);
        optionsPanel.Controls.Add(_stepInput, 3, 0);

        var refreshButton = NewButton("Refresh");
        refreshButton.Click += (_, _) => RefreshSessions();
        optionsPanel.Controls.Add(refreshButton, 1, 1);

        var addTargetButton = NewButton("AddToTargets");
        addTargetButton.Click += (_, _) => AddCurrentProcessAsTarget();
        optionsPanel.Controls.Add(addTargetButton, 3, 1);

        _startWithWindowsCheckBox.Tag = "StartWithWindows";
        _startWithWindowsCheckBox.Dock = DockStyle.Fill;
        _startWithWindowsCheckBox.CheckedChanged += (_, _) => SaveSettingsFromUi(registerHotkeys: false);
        optionsPanel.Controls.Add(_startWithWindowsCheckBox, 1, 2);
        optionsPanel.SetColumnSpan(_startWithWindowsCheckBox, 3);

        _routeHardwareVolumeCheckBox.Tag = "HardwareVolume";
        _routeHardwareVolumeCheckBox.Dock = DockStyle.Fill;
        _routeHardwareVolumeCheckBox.CheckedChanged += (_, _) => SaveSettingsFromUi(registerHotkeys: false);
        optionsPanel.Controls.Add(_routeHardwareVolumeCheckBox, 1, 3);
        optionsPanel.SetColumnSpan(_routeHardwareVolumeCheckBox, 2);

        _logKeyboardCheckBox.Tag = "LogKeys";
        _logKeyboardCheckBox.Dock = DockStyle.Fill;
        _logKeyboardCheckBox.CheckedChanged += (_, _) => SaveSettingsFromUi(registerHotkeys: false);
        optionsPanel.Controls.Add(_logKeyboardCheckBox, 3, 3);

        optionsPanel.Controls.Add(NewLabel("Language"), 0, 4);
        _languageCombo.Dock = DockStyle.Fill;
        _languageCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _languageCombo.SelectedIndexChanged += (_, _) => OnLanguageChanged();
        optionsPanel.Controls.Add(_languageCombo, 1, 4);

        var hotkeyPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(0, 8, 0, 8)
        };
        hotkeyPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        hotkeyPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 3; i++)
        {
            hotkeyPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        }

        root.Controls.Add(hotkeyPanel, 0, 1);

        AddHotkeyRow(hotkeyPanel, 0, "HotkeyUp", _volumeUpHotkey);
        AddHotkeyRow(hotkeyPanel, 1, "HotkeyDown", _volumeDownHotkey);
        AddHotkeyRow(hotkeyPanel, 2, "HotkeyMute", _toggleMuteHotkey);

        _volumeUpHotkey.ValueChangedByUser += (_, _) => SaveSettingsFromUi(registerHotkeys: true);
        _volumeDownHotkey.ValueChangedByUser += (_, _) => SaveSettingsFromUi(registerHotkeys: true);
        _toggleMuteHotkey.ValueChangedByUser += (_, _) => SaveSettingsFromUi(registerHotkeys: true);

        var contentPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
        contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(contentPanel, 0, 2);

        var targetsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        targetsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        targetsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        contentPanel.Controls.Add(targetsPanel, 0, 0);

        targetsPanel.Controls.Add(NewLabel("Targets"), 0, 0);
        _targetAppsList.Dock = DockStyle.Fill;
        _targetAppsList.CheckOnClick = true;
        _targetAppsList.ItemCheck += TargetAppsListOnItemCheck;
        targetsPanel.Controls.Add(_targetAppsList, 0, 1);

        _sessionsGrid.Dock = DockStyle.Fill;
        _sessionsGrid.AutoGenerateColumns = false;
        _sessionsGrid.AllowUserToAddRows = false;
        _sessionsGrid.AllowUserToDeleteRows = false;
        _sessionsGrid.ReadOnly = true;
        _sessionsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _sessionsGrid.MultiSelect = false;
        _sessionsGrid.DataSource = _sessions;
        AddGridColumn(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AudioSessionInfo.ProcessName), Width = 180 }, "Process");
        AddGridColumn(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AudioSessionInfo.ProcessId), Width = 80 }, "PID");
        AddGridColumn(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AudioSessionInfo.VolumePercent), Width = 100 }, "Volume");
        AddGridColumn(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(AudioSessionInfo.IsMuted), Width = 70 }, "Mute");
        AddGridColumn(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AudioSessionInfo.DisplayName), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill }, "SessionName");
        _sessionsGrid.CellDoubleClick += (_, _) => SelectGridProcess();
        contentPanel.Controls.Add(_sessionsGrid, 1, 0);

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_statusLabel, 0, 3);
    }

    private static Label NewLabel(string key)
    {
        return new Label { Tag = key, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    }

    private static Button NewButton(string key)
    {
        return new Button { Tag = key, Dock = DockStyle.Fill };
    }

    private static void AddHotkeyRow(TableLayoutPanel panel, int row, string labelKey, HotkeyTextBox textBox)
    {
        panel.Controls.Add(NewLabel(labelKey), 0, row);
        textBox.Dock = DockStyle.Fill;
        panel.Controls.Add(textBox, 1, row);
    }

    private void AddGridColumn(DataGridViewColumn column, string key)
    {
        column.Tag = key;
        _sessionsGrid.Columns.Add(column);
    }

    private void BuildTrayIcon()
    {
        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add(new ToolStripMenuItem { Tag = "Open" });
        _trayMenu.Items[0].Click += (_, _) => ShowMainWindow();
        _trayMenu.Items.Add(new ToolStripMenuItem { Tag = "RefreshSessions" });
        _trayMenu.Items[1].Click += (_, _) => RefreshSessions();
        _trayMenu.Items.Add(new ToolStripMenuItem { Tag = "About" });
        _trayMenu.Items[2].Click += (_, _) => MessageBox.Show(Localizer.T("AboutText"), "App Volume Hotkeys", MessageBoxButtons.OK, MessageBoxIcon.Information);
        _trayMenu.Items.Add(new ToolStripMenuItem { Tag = "Exit" });
        _trayMenu.Items[3].Click += (_, _) =>
        {
            _isExiting = true;
            Close();
        };

        _notifyIcon = new NotifyIcon
        {
            Icon = _appIcon,
            Text = "App Volume Hotkeys",
            Visible = true,
            ContextMenuStrip = _trayMenu
        };
        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private void StartHardwareVolumeRouter()
    {
        _hardwareVolumeKeyRouter.Enabled = _settings.RouteHardwareVolumeKeysToActiveProfile;
        _hardwareVolumeKeyRouter.LogKeyboardEvents = _settings.LogKeyboardEvents;
        _hardwareVolumeKeyRouter.VolumeKeyPressed += (_, e) =>
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            BeginInvoke(new Action(() => HandleHardwareVolumeAction(e.Action)));
        };
        _hardwareVolumeKeyRouter.Start();
    }

    private void LoadSettingsIntoUi()
    {
        _isLoadingUi = true;
        _stepInput.Value = Math.Clamp(_settings.VolumeStepPercent, (int)_stepInput.Minimum, (int)_stepInput.Maximum);
        _startWithWindowsCheckBox.Checked = _settings.StartWithWindows || AutoStartService.IsEnabled();
        _routeHardwareVolumeCheckBox.Checked = _settings.RouteHardwareVolumeKeysToActiveProfile;
        _logKeyboardCheckBox.Checked = _settings.LogKeyboardEvents;
        _volumeUpHotkey.SetValue(_settings.VolumeUp.ToDefinition());
        _volumeDownHotkey.SetValue(_settings.VolumeDown.ToDefinition());
        _toggleMuteHotkey.SetValue(_settings.ToggleMute.ToDefinition());
        _targetProcessCombo.Text = _settings.TargetProcessName;

        _languageCombo.Items.Clear();
        foreach (var option in Localizer.Options)
        {
            _languageCombo.Items.Add(option);
        }

        var selectedLanguage = Localizer.Options.FirstOrDefault(option => option.Code.Equals(_settings.Language, StringComparison.OrdinalIgnoreCase))
            ?? Localizer.Options[0];
        _languageCombo.SelectedItem = selectedLanguage;

        RebuildTargetAppsList();
        _isLoadingUi = false;

        SaveSettingsFromUi(registerHotkeys: false);
    }

    private void OnLanguageChanged()
    {
        if (_isLoadingUi || _languageCombo.SelectedItem is not LanguageOption option)
        {
            return;
        }

        _settings.Language = option.Code;
        Localizer.SetLanguage(option.Code);
        ApplyLocalization();
        SaveSettingsFromUi(registerHotkeys: false);
    }

    private void ApplyLocalization()
    {
        ApplyLocalizationToControls(Controls);
        foreach (DataGridViewColumn column in _sessionsGrid.Columns)
        {
            if (column.Tag is string key)
            {
                column.HeaderText = Localizer.T(key);
            }
        }

        if (_trayMenu is not null)
        {
            foreach (ToolStripMenuItem item in _trayMenu.Items)
            {
                if (item.Tag is string key)
                {
                    item.Text = Localizer.T(key);
                }
            }
        }

        _volumeUpHotkey.SetValue(_volumeUpHotkey.Value);
        _volumeDownHotkey.SetValue(_volumeDownHotkey.Value);
        _toggleMuteHotkey.SetValue(_toggleMuteHotkey.Value);
    }

    private static void ApplyLocalizationToControls(Control.ControlCollection controls)
    {
        foreach (Control control in controls)
        {
            if (control.Tag is string key)
            {
                control.Text = Localizer.T(key);
            }

            if (control.HasChildren)
            {
                ApplyLocalizationToControls(control.Controls);
            }
        }
    }

    private void RefreshSessions(bool keepStatus = false)
    {
        try
        {
            _isRefreshingSessions = true;
            var selected = !string.IsNullOrWhiteSpace(_targetProcessCombo.Text)
                ? _targetProcessCombo.Text
                : _settings.TargetProcessName;

            var sessions = _audioSessionService.GetSessions();

            _sessions.Clear();
            foreach (var session in sessions)
            {
                _sessions.Add(session);
            }

            var processes = sessions
                .Select(session => session.ProcessName)
                .Concat(_settings.Profiles.Select(profile => profile.ProcessName))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            if (!string.IsNullOrWhiteSpace(selected) && !processes.Contains(selected, StringComparer.OrdinalIgnoreCase))
            {
                processes.Insert(0, selected);
            }

            _targetProcessCombo.Items.Clear();
            foreach (var process in processes)
            {
                _targetProcessCombo.Items.Add(process);
            }

            if (!string.IsNullOrWhiteSpace(selected))
            {
                _targetProcessCombo.Text = processes.FirstOrDefault(process => string.Equals(process, selected, StringComparison.OrdinalIgnoreCase)) ?? selected;
            }
            else if (_targetProcessCombo.Items.Count > 0)
            {
                _targetProcessCombo.SelectedIndex = 0;
            }

            RebuildTargetAppsList();

            if (!keepStatus)
            {
                SetStatus(Localizer.Format("FoundSessions", sessions.Count, GetTargetProfiles().Count));
            }
        }
        catch (Exception ex)
        {
            SetStatus(Localizer.Format("RefreshError", ex.Message));
        }
        finally
        {
            _isRefreshingSessions = false;
        }
    }

    private void SelectGridProcess()
    {
        if (_sessionsGrid.CurrentRow?.DataBoundItem is not AudioSessionInfo session)
        {
            return;
        }

        _targetProcessCombo.Text = session.ProcessName;
        AddProcessAsTarget(session.ProcessName);
        SetStatus(Localizer.Format("AddedTarget", session.ProcessName));
    }

    private void AddCurrentProcessAsTarget()
    {
        AddProcessAsTarget(_targetProcessCombo.Text);
    }

    private void AddProcessAsTarget(string processName)
    {
        processName = NormalizeProcessName(processName);
        if (string.IsNullOrWhiteSpace(processName))
        {
            return;
        }

        var profile = GetProfile(processName, create: true);
        profile!.IsHotkeyTarget = true;
        profile.VolumeStepPercent = (int)_stepInput.Value;
        _settings.TargetProcessName = processName;
        _targetProcessCombo.Text = processName;
        RebuildTargetAppsList();
        SaveSettingsFromUi(registerHotkeys: false);
    }

    private void TargetAppsListOnItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (_isLoadingUi || _isRefreshingSessions)
        {
            return;
        }

        BeginInvoke(new Action(() =>
        {
            var processName = _targetAppsList.Items[e.Index]?.ToString() ?? string.Empty;
            var profile = GetProfile(processName, create: true);
            if (profile is null)
            {
                return;
            }

            profile.IsHotkeyTarget = _targetAppsList.GetItemChecked(e.Index);
            SaveSettingsFromUi(registerHotkeys: false);
        }));
    }

    private void RebuildTargetAppsList()
    {
        var processNames = _settings.Profiles
            .Select(profile => NormalizeProcessName(profile.ProcessName))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        _isLoadingUi = true;
        _targetAppsList.Items.Clear();
        foreach (var processName in processNames)
        {
            var profile = GetProfile(processName, create: false);
            _targetAppsList.Items.Add(processName, profile?.IsHotkeyTarget == true);
        }

        _isLoadingUi = false;
    }

    private void SaveSettingsFromUi(bool registerHotkeys)
    {
        if (_isLoadingUi)
        {
            return;
        }

        if (_languageCombo.SelectedItem is LanguageOption option)
        {
            _settings.Language = option.Code;
        }

        _settings.TargetProcessName = NormalizeProcessName(_targetProcessCombo.Text);
        _settings.VolumeStepPercent = (int)_stepInput.Value;
        _settings.StartWithWindows = _startWithWindowsCheckBox.Checked;
        _settings.RouteHardwareVolumeKeysToActiveProfile = _routeHardwareVolumeCheckBox.Checked;
        _settings.LogKeyboardEvents = _logKeyboardCheckBox.Checked;
        _settings.VolumeUp = HotkeySetting.FromDefinition(_volumeUpHotkey.Value);
        _settings.VolumeDown = HotkeySetting.FromDefinition(_volumeDownHotkey.Value);
        _settings.ToggleMute = HotkeySetting.FromDefinition(_toggleMuteHotkey.Value);

        for (var i = 0; i < _targetAppsList.Items.Count; i++)
        {
            var processName = _targetAppsList.Items[i]?.ToString() ?? string.Empty;
            var profile = GetProfile(processName, create: false);
            if (profile is not null)
            {
                profile.IsHotkeyTarget = _targetAppsList.GetItemChecked(i);
                profile.VolumeStepPercent = _settings.VolumeStepPercent;
            }
        }

        _hardwareVolumeKeyRouter.Enabled = _settings.RouteHardwareVolumeKeysToActiveProfile;
        _hardwareVolumeKeyRouter.LogKeyboardEvents = _settings.LogKeyboardEvents;
        AppSettingsStore.Save(_settings);
        ApplyAutoStartSetting();

        if (_settings.LogKeyboardEvents)
        {
            SetStatus(Localizer.Format("KeyboardLog", AppPaths.KeyboardLogPath));
        }

        if (registerHotkeys)
        {
            RegisterAllHotkeys();
        }
    }

    private void HandleHotkey(int id)
    {
        var action = id switch
        {
            HotkeyVolumeUp => VolumeAction.Up,
            HotkeyVolumeDown => VolumeAction.Down,
            HotkeyToggleMute => VolumeAction.ToggleMute,
            _ => (VolumeAction?)null
        };

        if (action is null)
        {
            return;
        }

        var summary = ExecuteAction(GetTargetProfiles(), action.Value);
        ShowActionSummary(summary, GetActionName(action.Value, summary.AllMuted));
    }

    private void HandleHardwareVolumeAction(HardwareVolumeAction action)
    {
        var processName = ForegroundProcessService.GetForegroundProcessName();
        var profile = GetTargetProfiles()
            .FirstOrDefault(item => string.Equals(item.ProcessName, processName, StringComparison.OrdinalIgnoreCase));
        if (profile is null)
        {
            return;
        }

        var volumeAction = action switch
        {
            HardwareVolumeAction.Up => VolumeAction.Up,
            HardwareVolumeAction.Down => VolumeAction.Down,
            HardwareVolumeAction.ToggleMute => VolumeAction.ToggleMute,
            _ => (VolumeAction?)null
        };

        if (volumeAction is null)
        {
            return;
        }

        var summary = ExecuteAction([profile], volumeAction.Value);
        ShowActionSummary(summary, GetActionName(volumeAction.Value, summary.AllMuted));
    }

    private ActionResultSummary ExecuteAction(IReadOnlyList<AppProfile> profiles, VolumeAction action)
    {
        if (profiles.Count == 0)
        {
            return new ActionResultSummary(0, 0, 0, false, Localizer.T("NoTargets"));
        }

        var changed = 0;
        var volumeTotal = 0;
        var allMuted = true;

        foreach (var profile in profiles)
        {
            try
            {
                var result = action switch
                {
                    VolumeAction.Up => _audioSessionService.ChangeVolume(profile.ProcessName, _settings.VolumeStepPercent),
                    VolumeAction.Down => _audioSessionService.ChangeVolume(profile.ProcessName, -_settings.VolumeStepPercent),
                    VolumeAction.ToggleMute => _audioSessionService.ToggleMute(profile.ProcessName),
                    _ => new AudioActionResult(false, 0, false)
                };

                if (!result.Changed)
                {
                    continue;
                }

                changed++;
                volumeTotal += result.VolumePercent;
                allMuted &= result.IsMuted;
            }
            catch (Exception ex)
            {
                SetStatus(Localizer.Format("VolumeError", profile.ProcessName, ex.Message));
            }
        }

        var title = profiles.Count == 1
            ? profiles[0].ProcessName
            : Localizer.AppCount(profiles.Count);
        var average = changed == 0 ? 0 : (int)Math.Round(volumeTotal / (double)changed);
        return new ActionResultSummary(changed, profiles.Count, average, allMuted, title);
    }

    private void ShowActionSummary(ActionResultSummary summary, string actionName)
    {
        if (summary.TargetCount == 0)
        {
            SetStatus(Localizer.T("NoTargetsStatus"));
            _overlayForm.ShowAction(Localizer.T("NoTargets"), Localizer.T("NoTargetsDetail"), 0, false);
            return;
        }

        if (summary.ChangedCount == 0)
        {
            SetStatus(Localizer.T("NoSessionStatus"));
            _overlayForm.ShowAction(summary.Title, Localizer.T("NoSession"), 0, false);
            return;
        }

        SetStatus($"{summary.Title}: {actionName}, {summary.AverageVolumePercent}%");
        _overlayForm.ShowAction(summary.Title, actionName, summary.AverageVolumePercent, summary.AllMuted);
        RefreshSessions(keepStatus: true);
    }

    private void RegisterAllHotkeys()
    {
        UnregisterAllHotkeys();
        var failed = 0;
        failed += RegisterHotkey(HotkeyVolumeUp, _settings.VolumeUp.ToDefinition(), Localizer.T("ActionUp")) ? 0 : 1;
        failed += RegisterHotkey(HotkeyVolumeDown, _settings.VolumeDown.ToDefinition(), Localizer.T("ActionDown")) ? 0 : 1;
        failed += RegisterHotkey(HotkeyToggleMute, _settings.ToggleMute.ToDefinition(), "mute") ? 0 : 1;

        if (failed == 0)
        {
            SetStatus(Localizer.Format("HotkeysRegistered", GetTargetProfiles().Count));
        }
    }

    private bool RegisterHotkey(int id, HotkeyDefinition hotkey, string actionName)
    {
        if (!hotkey.IsValid)
        {
            return true;
        }

        if (GlobalHotkeys.RegisterHotKey(Handle, id, hotkey.Modifiers, hotkey.Key))
        {
            return true;
        }

        var error = Marshal.GetLastWin32Error();
        SetStatus(Localizer.Format("HotkeyRegisterFailed", actionName, hotkey, error));
        return false;
    }

    private void UnregisterAllHotkeys()
    {
        GlobalHotkeys.UnregisterHotKey(Handle, HotkeyVolumeUp);
        GlobalHotkeys.UnregisterHotKey(Handle, HotkeyVolumeDown);
        GlobalHotkeys.UnregisterHotKey(Handle, HotkeyToggleMute);
    }

    private IReadOnlyList<AppProfile> GetTargetProfiles()
    {
        return _settings.Profiles
            .Where(profile => profile.IsHotkeyTarget && !string.IsNullOrWhiteSpace(profile.ProcessName))
            .ToList();
    }

    private AppProfile? GetProfile(string processName, bool create)
    {
        processName = NormalizeProcessName(processName);
        if (string.IsNullOrWhiteSpace(processName))
        {
            return null;
        }

        var profile = _settings.Profiles.FirstOrDefault(item => string.Equals(item.ProcessName, processName, StringComparison.OrdinalIgnoreCase));
        if (profile is not null || !create)
        {
            return profile;
        }

        profile = new AppProfile
        {
            ProcessName = processName,
            VolumeStepPercent = _settings.VolumeStepPercent,
            IsHotkeyTarget = true
        };
        _settings.Profiles.Add(profile);
        return profile;
    }

    private void MigrateLegacySettings()
    {
        foreach (var profile in _settings.Profiles)
        {
            profile.ProcessName = NormalizeProcessName(profile.ProcessName);
            if (profile.VolumeUp.ToDefinition().IsValid || profile.VolumeDown.ToDefinition().IsValid || profile.ToggleMute.ToDefinition().IsValid)
            {
                profile.IsHotkeyTarget = true;
            }
        }

        if (_settings.Profiles.Count > 0 || string.IsNullOrWhiteSpace(_settings.TargetProcessName))
        {
            return;
        }

        _settings.Profiles.Add(new AppProfile
        {
            ProcessName = NormalizeProcessName(_settings.TargetProcessName),
            VolumeStepPercent = _settings.VolumeStepPercent,
            IsHotkeyTarget = true
        });
    }

    private void ApplyAutoStartSetting()
    {
        try
        {
            if (AutoStartService.IsEnabled() != _settings.StartWithWindows)
            {
                AutoStartService.SetEnabled(_settings.StartWithWindows);
            }
        }
        catch (Exception ex)
        {
            SetStatus(Localizer.Format("AutostartFailed", ex.Message));
        }
    }

    private void ShowMainWindow()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        RefreshSessions();
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
    }

    private static string GetActionName(VolumeAction action, bool isMuted)
    {
        return action switch
        {
            VolumeAction.Up => Localizer.T("ActionUp"),
            VolumeAction.Down => Localizer.T("ActionDown"),
            VolumeAction.ToggleMute => isMuted ? Localizer.T("ActionMuteOn") : Localizer.T("ActionMuteOff"),
            _ => Localizer.T("Action")
        };
    }

    private static string NormalizeProcessName(string processName)
    {
        processName = processName.Trim();
        if (string.IsNullOrWhiteSpace(processName))
        {
            return string.Empty;
        }

        return processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? processName
            : processName + ".exe";
    }
}

using System.Drawing;
using MilkyFrog.Core.Abstractions;
using MilkyFrog.Core.Models;

namespace MilkyFrog.Platform.Windows.Tray;

public sealed class WindowsTrayService : ITrayService
{
    private const string HideText = "\u9690\u85cf\u5976\u86d9";
    private const string ShowText = "\u663e\u793a\u5976\u86d9";
    private const string MuteText = "\u9759\u97f3";
    private const string UnmuteText = "\u53d6\u6d88\u9759\u97f3";
    private const string AudioOutputText = "\u97f3\u9891\u8f93\u51fa";
    private const string ExitText = "\u9000\u51fa";
    private const string TrayText = "\u5976\u86d9";

    private NotifyIcon? _notifyIcon;
    private Icon? _trayIcon;
    private ToolStripMenuItem? _visibilityMenuItem;
    private ToolStripMenuItem? _muteMenuItem;
    private ToolStripMenuItem? _audioOutputMenuItem;

    private bool _isPetVisible;
    private bool _isMuted;
    private bool _isDisposed;

    public event EventHandler? ShowRequested;

    public event EventHandler? HideRequested;

    public event EventHandler? MuteRequested;

    public event EventHandler<AudioOutputDeviceRequestedEventArgs>?
        AudioOutputDeviceRequested;

    public event EventHandler? ExitRequested;

    public void Initialize()
    {
        ObjectDisposedException.ThrowIf(
            _isDisposed,
            this);

        if (_notifyIcon is not null)
        {
            return;
        }

        _visibilityMenuItem =
            new ToolStripMenuItem(
                HideText,
                image: null,
                (_, _) =>
                    HandleVisibilityMenuClick());

        _muteMenuItem =
            new ToolStripMenuItem(
                MuteText,
                image: null,
                (_, _) =>
                    MuteRequested?.Invoke(
                        this,
                        EventArgs.Empty));

        _audioOutputMenuItem =
            new ToolStripMenuItem(
                AudioOutputText);

        var exitMenuItem =
            new ToolStripMenuItem(
                ExitText,
                image: null,
                (_, _) =>
                    ExitRequested?.Invoke(
                        this,
                        EventArgs.Empty));

        var menu = new ContextMenuStrip();
        menu.Items.Add(_visibilityMenuItem);
        menu.Items.Add(_muteMenuItem);
        menu.Items.Add(_audioOutputMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitMenuItem);

        _notifyIcon = new NotifyIcon
        {
            Text = TrayText,
            Icon = (_trayIcon = LoadTrayIcon()) ?? SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true
        };

        _notifyIcon.MouseClick +=
            NotifyIcon_MouseClick;
    }

    public void SetPetVisible(bool isVisible)
    {
        _isPetVisible = isVisible;

        if (_visibilityMenuItem is not null)
        {
            _visibilityMenuItem.Text =
                isVisible ? HideText : ShowText;
        }
    }

    public void SetMuted(bool isMuted)
    {
        _isMuted = isMuted;

        if (_muteMenuItem is null)
        {
            return;
        }

        _muteMenuItem.Text =
            isMuted ? UnmuteText : MuteText;

        _muteMenuItem.Checked = isMuted;
    }

    public void SetAudioOutputDevices(
        IReadOnlyList<AudioOutputDevice> devices,
        string selectedDeviceId)
    {
        if (_audioOutputMenuItem is null)
        {
            return;
        }

        _audioOutputMenuItem.DropDownItems.Clear();

        foreach (AudioOutputDevice device in devices)
        {
            var deviceMenuItem = new ToolStripMenuItem(
                device.Name)
            {
                Checked = string.Equals(
                    device.Id,
                    selectedDeviceId,
                    StringComparison.Ordinal),
                Tag = device.Id
            };

            deviceMenuItem.Click +=
                AudioOutputMenuItem_Click;

            _audioOutputMenuItem.DropDownItems.Add(
                deviceMenuItem);
        }

        _audioOutputMenuItem.Enabled =
            devices.Count > 0;
    }

    private void AudioOutputMenuItem_Click(
        object? sender,
        EventArgs e)
    {
        if (sender is not ToolStripMenuItem menuItem ||
            menuItem.Tag is not string deviceId)
        {
            return;
        }

        AudioOutputDeviceRequested?.Invoke(
            this,
            new AudioOutputDeviceRequestedEventArgs(
                deviceId));
    }

    private void NotifyIcon_MouseClick(
        object? sender,
        MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        ShowRequested?.Invoke(
            this,
            EventArgs.Empty);
    }

    private void HandleVisibilityMenuClick()
    {
        if (_isPetVisible)
        {
            HideRequested?.Invoke(
                this,
                EventArgs.Empty);

            return;
        }

        ShowRequested?.Invoke(
            this,
            EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (_notifyIcon is not null)
        {
            _notifyIcon.MouseClick -=
                NotifyIcon_MouseClick;

            _notifyIcon.Visible = false;
            _notifyIcon.ContextMenuStrip?.Dispose();
            _notifyIcon.Dispose();
        _notifyIcon = null;

        _trayIcon?.Dispose();
        _trayIcon = null;
        }

        _visibilityMenuItem = null;
        _muteMenuItem = null;
        _audioOutputMenuItem = null;

        ShowRequested = null;
        HideRequested = null;
        MuteRequested = null;
        AudioOutputDeviceRequested = null;
        ExitRequested = null;
    }

    private static Icon? LoadTrayIcon()
    {
        string iconPath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Icons",
            "milky-frog.ico");

        if (!File.Exists(iconPath))
        {
            return null;
        }

        try
        {
            return new Icon(iconPath);
        }
        catch (Exception) when (
            OperatingSystem.IsWindows())
        {
            return null;
        }
    }
}

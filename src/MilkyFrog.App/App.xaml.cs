using System.Windows;
using MilkyFrog.App.Composition;
using MilkyFrog.Core.Abstractions;
using MilkyFrog.Core.Models;
using MilkyFrog.Core.Services;
using MilkyFrog.Platform.Windows.Audio;
using MilkyFrog.Platform.Windows.Cursor;
using MilkyFrog.Platform.Windows.SingleInstance;
using MilkyFrog.Platform.Windows.Tray;

namespace MilkyFrog.App;

public partial class App : Application
{
    private ISingleInstanceService? _singleInstanceService;
    private ITrayService? _trayService;
    private IAudioService? _audioService;
    private MainWindow? _mainWindow;

    private bool _isExiting;

    protected override void OnStartup(
        StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceService =
            new WindowsSingleInstanceService();

        if (!_singleInstanceService.IsPrimaryInstance)
        {
            _singleInstanceService.SignalPrimaryInstance();
            _singleInstanceService.Dispose();
            _singleInstanceService = null;

            Shutdown();
            return;
        }

        _singleInstanceService.ActivationRequested +=
            SingleInstanceService_ActivationRequested;

        var cursorPositionProvider =
            new WindowsCursorPositionProvider();

        var eyeMovementCalculator =
            new EyeMovementCalculator();

        var assetCatalog =
            new AssetCatalog();

        _audioService =
            new WindowsAudioService();

        _mainWindow = new MainWindow(
            cursorPositionProvider,
            eyeMovementCalculator,
            assetCatalog,
            _audioService);

        MainWindow = _mainWindow;

        _trayService =
            new WindowsTrayService();

        _trayService.ShowRequested +=
            TrayService_ShowRequested;

        _trayService.HideRequested +=
            TrayService_HideRequested;

        _trayService.MuteRequested +=
            TrayService_MuteRequested;

        _trayService.AudioOutputDeviceRequested +=
            TrayService_AudioOutputDeviceRequested;

        _trayService.ExitRequested +=
            TrayService_ExitRequested;

        _trayService.Initialize();

        _mainWindow.IsVisibleChanged +=
            MainWindow_IsVisibleChanged;

        _mainWindow.Show();

        _trayService.SetPetVisible(true);
        _trayService.SetMuted(
            _audioService.IsMuted);

        RefreshAudioOutputDevices();

        _singleInstanceService.StartListening();
    }

    private void SingleInstanceService_ActivationRequested(
        object? sender,
        EventArgs e)
    {
        Dispatcher.BeginInvoke(ShowPet);
    }

    private void TrayService_ShowRequested(
        object? sender,
        EventArgs e)
    {
        ShowPet();
    }

    private void TrayService_HideRequested(
        object? sender,
        EventArgs e)
    {
        HidePet();
    }

    private void TrayService_MuteRequested(
        object? sender,
        EventArgs e)
    {
        if (_audioService is null ||
            _trayService is null)
        {
            return;
        }

        bool muted = !_audioService.IsMuted;

        _audioService.SetMuted(muted);
        _trayService.SetMuted(muted);
    }

    private void TrayService_ExitRequested(
        object? sender,
        EventArgs e)
    {
        ExitApplication();
    }

    private void TrayService_AudioOutputDeviceRequested(
        object? sender,
        AudioOutputDeviceRequestedEventArgs e)
    {
        if (_audioService is null)
        {
            return;
        }

        if (_audioService.SelectOutputDevice(
                e.DeviceId))
        {
            RefreshAudioOutputDevices();
        }
    }

    private void RefreshAudioOutputDevices()
    {
        if (_audioService is null ||
            _trayService is null)
        {
            return;
        }

        _trayService.SetAudioOutputDevices(
            _audioService.GetOutputDevices(),
            _audioService.SelectedOutputDeviceId);
    }

    private void ShowPet()
    {
        if (_isExiting ||
            _mainWindow is null ||
            _mainWindow.IsVisible)
        {
            return;
        }

        _mainWindow.Show();
    }

    private void HidePet()
    {
        if (_isExiting ||
            _mainWindow is null ||
            !_mainWindow.IsVisible)
        {
            return;
        }

        _mainWindow.Hide();
    }

    private void MainWindow_IsVisibleChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        _trayService?.SetPetVisible(
            _mainWindow?.IsVisible == true);
    }

    public void ExitApplication()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;

        _mainWindow?.PrepareForApplicationExit();

        Shutdown();
    }

    protected override void OnExit(
        ExitEventArgs e)
    {
        if (_mainWindow is not null)
        {
            _mainWindow.IsVisibleChanged -=
                MainWindow_IsVisibleChanged;
        }

        if (_trayService is not null)
        {
            _trayService.ShowRequested -=
                TrayService_ShowRequested;

            _trayService.HideRequested -=
                TrayService_HideRequested;

            _trayService.MuteRequested -=
                TrayService_MuteRequested;

            _trayService.AudioOutputDeviceRequested -=
                TrayService_AudioOutputDeviceRequested;

            _trayService.ExitRequested -=
                TrayService_ExitRequested;

            _trayService.Dispose();
            _trayService = null;
        }

        _audioService?.Dispose();
        _audioService = null;

        if (_singleInstanceService is not null)
        {
            _singleInstanceService.ActivationRequested -=
                SingleInstanceService_ActivationRequested;

            _singleInstanceService.Dispose();
            _singleInstanceService = null;
        }

        base.OnExit(e);
    }
}

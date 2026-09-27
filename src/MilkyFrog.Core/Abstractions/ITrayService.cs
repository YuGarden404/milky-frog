using MilkyFrog.Core.Models;

namespace MilkyFrog.Core.Abstractions;

public interface ITrayService : IDisposable
{
    event EventHandler? ShowRequested;

    event EventHandler? HideRequested;

    event EventHandler? MuteRequested;

    event EventHandler<AudioOutputDeviceRequestedEventArgs>?
        AudioOutputDeviceRequested;

    event EventHandler? ExitRequested;

    void Initialize();

    void SetPetVisible(bool isVisible);

    void SetMuted(bool isMuted);

    void SetAudioOutputDevices(
        IReadOnlyList<AudioOutputDevice> devices,
        string selectedDeviceId);
}

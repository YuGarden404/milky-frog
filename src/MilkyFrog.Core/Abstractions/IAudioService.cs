using MilkyFrog.Core.Models;

namespace MilkyFrog.Core.Abstractions;

public interface IAudioService : IDisposable
{
    bool IsMuted { get; }

    float Volume { get; }

    bool IsPlaying { get; }

    string SelectedOutputDeviceId { get; }

    event EventHandler? PlaybackCompleted;

    bool Play(string relativePath);

    IReadOnlyList<AudioOutputDevice> GetOutputDevices();

    bool SelectOutputDevice(string deviceId);

    void Stop();

    void SetMuted(bool muted);

    void SetVolume(float volume);
}

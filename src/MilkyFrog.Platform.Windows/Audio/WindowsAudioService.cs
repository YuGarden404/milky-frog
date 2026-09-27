using MilkyFrog.Core.Abstractions;
using MilkyFrog.Core.Models;
using NAudio;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace MilkyFrog.Platform.Windows.Audio;

public sealed class WindowsAudioService : IAudioService
{
    public const string SystemDefaultDeviceId = "default";

    private const string WaveOutDevicePrefix = "waveout:";

    private readonly object _syncRoot = new();

    private WaveOutEvent? _outputDevice;
    private AudioFileReader? _audioReader;

    private string _selectedOutputDeviceId =
        SystemDefaultDeviceId;

    private float _volume = 1.0f;
    private bool _isMuted;
    private bool _isDisposed;
    private bool _stopRequested;

    public bool IsMuted
    {
        get
        {
            lock (_syncRoot)
            {
                return _isMuted;
            }
        }
    }

    public float Volume
    {
        get
        {
            lock (_syncRoot)
            {
                return _volume;
            }
        }
    }

    public bool IsPlaying
    {
        get
        {
            lock (_syncRoot)
            {
                return _outputDevice?.PlaybackState ==
                    PlaybackState.Playing;
            }
        }
    }

    public string SelectedOutputDeviceId
    {
        get
        {
            lock (_syncRoot)
            {
                return _selectedOutputDeviceId;
            }
        }
    }

    public event EventHandler? PlaybackCompleted;

    public IReadOnlyList<AudioOutputDevice> GetOutputDevices()
    {
        ObjectDisposedException.ThrowIf(
            _isDisposed,
            this);

        var devices = new List<AudioOutputDevice>
        {
            new(
                SystemDefaultDeviceId,
                GetSystemDefaultDeviceName(),
                IsSystemDefault: true)
        };

        for (int index = 0;
             index < WaveOut.DeviceCount;
             index++)
        {
            WaveOutCapabilities capabilities =
                WaveOut.GetCapabilities(index);

            devices.Add(
                new AudioOutputDevice(
                    $"{WaveOutDevicePrefix}{index}",
                    capabilities.ProductName,
                    IsSystemDefault: false));
        }

        return devices;
    }

    public bool SelectOutputDevice(string deviceId)
    {
        ObjectDisposedException.ThrowIf(
            _isDisposed,
            this);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            deviceId);

        bool exists = GetOutputDevices().Any(
            device => string.Equals(
                device.Id,
                deviceId,
                StringComparison.Ordinal));

        if (!exists)
        {
            return false;
        }

        lock (_syncRoot)
        {
            if (string.Equals(
                    _selectedOutputDeviceId,
                    deviceId,
                    StringComparison.Ordinal))
            {
                return true;
            }

            _stopRequested = true;
            ReleasePlayback();
            _selectedOutputDeviceId = deviceId;
            return true;
        }
    }

    public bool Play(string relativePath)
    {
        ObjectDisposedException.ThrowIf(
            _isDisposed,
            this);

        string fullPath = Path.Combine(
            AppContext.BaseDirectory,
            relativePath.Replace(
                '/',
                Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath))
        {
            return false;
        }

        lock (_syncRoot)
        {
            _stopRequested = true;
            ReleasePlayback();

            try
            {
                _audioReader =
                    new AudioFileReader(fullPath);

                ApplyEffectiveVolume();

                _outputDevice = new WaveOutEvent
                {
                    DeviceNumber = ResolveDeviceNumber(
                        _selectedOutputDeviceId),
                    DesiredLatency = 100,
                    NumberOfBuffers = 3
                };

                _outputDevice.PlaybackStopped +=
                    OutputDevice_PlaybackStopped;

                _outputDevice.Init(_audioReader);

                _stopRequested = false;
                _outputDevice.Play();
                return true;
            }
            catch (Exception exception)
                when (
                    exception is IOException or
                    UnauthorizedAccessException or
                    InvalidOperationException or
                    ArgumentException or
                    MmException)
            {
                _stopRequested = true;
                ReleasePlayback();
                return false;
            }
        }
    }

    public void Stop()
    {
        if (_isDisposed)
        {
            return;
        }

        lock (_syncRoot)
        {
            _stopRequested = true;
            ReleasePlayback();
        }
    }

    public void SetMuted(bool muted)
    {
        ObjectDisposedException.ThrowIf(
            _isDisposed,
            this);

        lock (_syncRoot)
        {
            _isMuted = muted;
            ApplyEffectiveVolume();
        }
    }

    public void SetVolume(float volume)
    {
        ObjectDisposedException.ThrowIf(
            _isDisposed,
            this);

        if (!float.IsFinite(volume) ||
            volume is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(volume),
                "Volume must be between 0 and 1.");
        }

        lock (_syncRoot)
        {
            _volume = volume;
            ApplyEffectiveVolume();
        }
    }

    private static string GetSystemDefaultDeviceName()
    {
        try
        {
            using var enumerator =
                new MMDeviceEnumerator();

            using MMDevice endpoint =
                enumerator.GetDefaultAudioEndpoint(
                    DataFlow.Render,
                    Role.Multimedia);

            return $"System default ({endpoint.FriendlyName})";
        }
        catch
        {
            return "System default";
        }
    }

    private static int ResolveDeviceNumber(
        string deviceId)
    {
        if (string.Equals(
                deviceId,
                SystemDefaultDeviceId,
                StringComparison.Ordinal))
        {
            return -1;
        }

        if (!deviceId.StartsWith(
                WaveOutDevicePrefix,
                StringComparison.Ordinal))
        {
            return -1;
        }

        string indexText = deviceId[
            WaveOutDevicePrefix.Length..];

        return int.TryParse(
            indexText,
            out int deviceNumber)
            ? deviceNumber
            : -1;
    }

    private void ApplyEffectiveVolume()
    {
        if (_audioReader is null)
        {
            return;
        }

        _audioReader.Volume =
            _isMuted ? 0 : _volume;
    }

    private void OutputDevice_PlaybackStopped(
        object? sender,
        StoppedEventArgs e)
    {
        bool shouldRaiseCompleted;

        lock (_syncRoot)
        {
            shouldRaiseCompleted =
                !_stopRequested &&
                !_isDisposed &&
                e.Exception is null;

            ReleasePlayback();
        }

        if (shouldRaiseCompleted)
        {
            PlaybackCompleted?.Invoke(
                this,
                EventArgs.Empty);
        }
    }

    private void ReleasePlayback()
    {
        WaveOutEvent? outputDevice =
            _outputDevice;

        AudioFileReader? audioReader =
            _audioReader;

        _outputDevice = null;
        _audioReader = null;

        if (outputDevice is not null)
        {
            outputDevice.PlaybackStopped -=
                OutputDevice_PlaybackStopped;

            try
            {
                if (outputDevice.PlaybackState !=
                    PlaybackState.Stopped)
                {
                    outputDevice.Stop();
                }
            }
            catch (MmException)
            {
                // The endpoint may disappear while the app is running.
            }

            outputDevice.Dispose();
        }

        audioReader?.Dispose();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        lock (_syncRoot)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _stopRequested = true;
            ReleasePlayback();
        }

        PlaybackCompleted = null;
    }
}

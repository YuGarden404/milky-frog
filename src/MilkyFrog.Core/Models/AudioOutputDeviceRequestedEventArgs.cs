namespace MilkyFrog.Core.Models;

public sealed class AudioOutputDeviceRequestedEventArgs : EventArgs
{
    public AudioOutputDeviceRequestedEventArgs(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        DeviceId = deviceId;
    }

    public string DeviceId { get; }
}

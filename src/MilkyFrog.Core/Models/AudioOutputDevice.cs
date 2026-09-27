namespace MilkyFrog.Core.Models;

public sealed record AudioOutputDevice(
    string Id,
    string Name,
    bool IsSystemDefault);

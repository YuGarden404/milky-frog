namespace MilkyFrog.Core.Settings;

public sealed record AppSettings
{
    public const float DefaultVolume = 1.0f;

    public int Version { get; init; } = 1;

    public bool IsMuted { get; init; }

    public float Volume { get; init; } =
        DefaultVolume;

    public int? WindowLeft { get; init; }

    public int? WindowTop { get; init; }

    public AppSettings Normalize()
    {
        float normalizedVolume =
            float.IsFinite(Volume)
                ? Math.Clamp(Volume, 0, 1)
                : DefaultVolume;

        bool hasCompleteWindowPosition =
            WindowLeft.HasValue &&
            WindowTop.HasValue;

        return this with
        {
            Volume = normalizedVolume,
            WindowLeft = hasCompleteWindowPosition
                ? WindowLeft
                : null,
            WindowTop = hasCompleteWindowPosition
                ? WindowTop
                : null
        };
    }
}
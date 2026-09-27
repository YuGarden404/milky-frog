namespace MilkyFrog.Core.Animation;

public sealed record AnimationFrame(
    string AssetPath,
    TimeSpan Duration);
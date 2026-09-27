namespace MilkyFrog.Core.Animation;

public sealed class AnimationClip
{
    public AnimationClip(
        string name,
        IReadOnlyList<AnimationFrame> frames,
        bool loop)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "动画名称不能为空。",
                nameof(name));
        }

        if (frames is null ||
            frames.Count == 0)
        {
            throw new ArgumentException(
                "动画至少需要一帧。",
                nameof(frames));
        }

        if (frames.Any(frame =>
                string.IsNullOrWhiteSpace(frame.AssetPath) ||
                frame.Duration <= TimeSpan.Zero))
        {
            throw new ArgumentException(
                "动画帧路径和持续时间必须有效。",
                nameof(frames));
        }

        Name = name;
        Frames = frames;
        Loop = loop;
    }

    public string Name { get; }

    public IReadOnlyList<AnimationFrame> Frames { get; }

    public bool Loop { get; }

    public TimeSpan TotalDuration =>
        TimeSpan.FromTicks(
            Frames.Sum(frame =>
                frame.Duration.Ticks));
}
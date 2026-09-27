using MilkyFrog.Core.Animation;

namespace MilkyFrog.Core.Tests.Animation;

public sealed class AnimationClipTests
{
    [Fact]
    public void Constructor_ComputesTotalDuration()
    {
        var clip = new AnimationClip(
            "Test",
            new[]
            {
                new AnimationFrame(
                    "a.png",
                    TimeSpan.FromMilliseconds(100)),

                new AnimationFrame(
                    "b.png",
                    TimeSpan.FromMilliseconds(150))
            },
            loop: true);

        Assert.Equal(
            TimeSpan.FromMilliseconds(250),
            clip.TotalDuration);
    }

    [Fact]
    public void Constructor_RejectsEmptyFrames()
    {
        Assert.Throws<ArgumentException>(
            () => new AnimationClip(
                "Test",
                Array.Empty<AnimationFrame>(),
                loop: true));
    }

    [Fact]
    public void Constructor_RejectsInvalidDuration()
    {
        Assert.Throws<ArgumentException>(
            () => new AnimationClip(
                "Test",
                new[]
                {
                    new AnimationFrame(
                        "a.png",
                        TimeSpan.Zero)
                },
                loop: true));
    }
}
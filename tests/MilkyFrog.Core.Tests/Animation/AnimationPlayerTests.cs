using MilkyFrog.Core.Animation;

namespace MilkyFrog.Core.Tests.Animation;

public sealed class AnimationPlayerTests
{
    [Fact]
    public void Play_StartsAtFirstFrame()
    {
        var clip = CreateClip(loop: true);
        var player = new AnimationPlayer();

        player.Play(clip);

        Assert.True(player.IsPlaying);
        Assert.False(player.IsCompleted);
        Assert.Equal(0, player.CurrentFrameIndex);
        Assert.Equal(
            "frame-0.png",
            player.CurrentFrame?.AssetPath);
    }

    [Fact]
    public void Advance_MovesToNextFrameAfterDuration()
    {
        var clip = CreateClip(loop: true);
        var player = new AnimationPlayer();

        player.Play(clip);

        bool changed = player.Advance(
            TimeSpan.FromMilliseconds(100));

        Assert.True(changed);
        Assert.Equal(1, player.CurrentFrameIndex);
    }

    [Fact]
    public void Advance_LoopedClip_ReturnsToFirstFrame()
    {
        var clip = CreateClip(loop: true);
        var player = new AnimationPlayer();

        player.Play(clip);

        player.Advance(
            TimeSpan.FromMilliseconds(300));

        Assert.True(player.IsPlaying);
        Assert.False(player.IsCompleted);
        Assert.Equal(0, player.CurrentFrameIndex);
    }

    [Fact]
    public void Advance_NonLoopedClip_CompletesAtLastFrame()
    {
        var clip = CreateClip(loop: false);
        var player = new AnimationPlayer();

        player.Play(clip);

        player.Advance(
            TimeSpan.FromMilliseconds(300));

        Assert.False(player.IsPlaying);
        Assert.True(player.IsCompleted);
        Assert.Equal(
            clip.Frames.Count - 1,
            player.CurrentFrameIndex);
    }

    [Fact]
    public void Advance_RejectsNegativeDuration()
    {
        var clip = CreateClip(loop: true);
        var player = new AnimationPlayer();

        player.Play(clip);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => player.Advance(
                TimeSpan.FromMilliseconds(-1)));
    }

    private static AnimationClip CreateClip(
        bool loop)
    {
        return new AnimationClip(
            "Test",
            new[]
            {
                new AnimationFrame(
                    "frame-0.png",
                    TimeSpan.FromMilliseconds(100)),

                new AnimationFrame(
                    "frame-1.png",
                    TimeSpan.FromMilliseconds(100)),

                new AnimationFrame(
                    "frame-2.png",
                    TimeSpan.FromMilliseconds(100))
            },
            loop);
    }
}
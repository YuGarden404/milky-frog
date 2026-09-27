namespace MilkyFrog.Core.Animation;

public sealed class AnimationPlayer
{
    private AnimationClip? _clip;
    private int _currentFrameIndex;
    private TimeSpan _elapsedInFrame;

    public AnimationClip? Clip => _clip;

    public AnimationFrame? CurrentFrame =>
        _clip is null
            ? null
            : _clip.Frames[_currentFrameIndex];

    public int CurrentFrameIndex =>
        _currentFrameIndex;

    public bool IsPlaying { get; private set; }

    public bool IsCompleted { get; private set; }

    public void Play(
        AnimationClip clip,
        bool restart = true)
    {
        ArgumentNullException.ThrowIfNull(clip);

        if (!restart &&
            ReferenceEquals(_clip, clip))
        {
            IsPlaying = true;
            IsCompleted = false;
            return;
        }

        _clip = clip;
        _currentFrameIndex = 0;
        _elapsedInFrame = TimeSpan.Zero;
        IsPlaying = true;
        IsCompleted = false;
    }

    public void Stop()
    {
        IsPlaying = false;
    }

    public bool Advance(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elapsed));
        }

        if (!IsPlaying ||
            _clip is null ||
            elapsed == TimeSpan.Zero)
        {
            return false;
        }

        bool frameChanged = false;
        TimeSpan remaining = elapsed;

        while (remaining > TimeSpan.Zero &&
               IsPlaying)
        {
            AnimationFrame currentFrame =
                _clip.Frames[_currentFrameIndex];

            TimeSpan remainingInFrame =
                currentFrame.Duration -
                _elapsedInFrame;

            if (remaining < remainingInFrame)
            {
                _elapsedInFrame += remaining;
                remaining = TimeSpan.Zero;
                continue;
            }

            remaining -= remainingInFrame;
            _elapsedInFrame = TimeSpan.Zero;

            if (_currentFrameIndex <
                _clip.Frames.Count - 1)
            {
                _currentFrameIndex++;
                frameChanged = true;
                continue;
            }

            if (_clip.Loop)
            {
                _currentFrameIndex = 0;
                frameChanged = true;
                continue;
            }

            IsPlaying = false;
            IsCompleted = true;
        }

        return frameChanged;
    }
}
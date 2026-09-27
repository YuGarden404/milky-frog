using MilkyFrog.Core.Animation;
using MilkyFrog.Core.Assets;

namespace MilkyFrog.App.Composition;

public sealed class AssetCatalog : IAssetCatalog
{
    private const int IdleFrameCount = 33;
    private const int LaughFrameCount = 171;

    private readonly Dictionary<
        AnimationState,
        AnimationClip> _clips;

    private readonly Dictionary<
        string,
        string> _audioAssets;

    public AssetCatalog()
    {
        _clips = new()
        {
            [AnimationState.Idle] =
                CreateIdleClip(),

            [AnimationState.Laughing] =
                CreateLaughClip()
        };

        _audioAssets = new(
            StringComparer.OrdinalIgnoreCase)
        {
            ["Laugh"] = "Assets/Audio/laugh.wav"
        };
    }

    public AnimationClip GetClip(
        AnimationState state)
    {
        return _clips[state];
    }

    public bool TryGetAudio(
        string audioName,
        out string assetPath)
    {
        return _audioAssets.TryGetValue(
            audioName,
            out assetPath!);
    }

    private static AnimationClip CreateIdleClip()
    {
        AnimationFrame[] frames =
            Enumerable.Range(0, IdleFrameCount)
                .Concat(Enumerable.Range(1, IdleFrameCount - 2).Reverse())
                .Select(index =>
                    new AnimationFrame(
                        $"Assets/Character/Idle/idle_{index:D3}.png",
                        TimeSpan.FromSeconds(1d / 20)))
                .ToArray();

        return new AnimationClip(
            "Idle",
            frames,
            loop: true);
    }

    private static AnimationClip CreateLaughClip()
    {
        AnimationFrame[] frames =
            Enumerable.Range(0, LaughFrameCount)
                .Select(index =>
                    new AnimationFrame(
                        $"Assets/Character/Laugh/laugh_{index:D3}.png",
                        TimeSpan.FromSeconds(1d / 15)))
                .ToArray();

        return new AnimationClip(
            "Laughing",
            frames,
            loop: false);
    }
}

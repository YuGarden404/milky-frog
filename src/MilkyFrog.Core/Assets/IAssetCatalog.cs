using MilkyFrog.Core.Animation;

namespace MilkyFrog.Core.Assets;

public interface IAssetCatalog
{
    AnimationClip GetClip(AnimationState state);

    bool TryGetAudio(
        string audioName,
        out string assetPath);
}
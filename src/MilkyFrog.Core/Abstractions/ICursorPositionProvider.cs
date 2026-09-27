using MilkyFrog.Core.Models;

namespace MilkyFrog.Core.Abstractions;

public interface ICursorPositionProvider
{
    bool TryGetPosition(out Point2D position);
}
namespace MilkyFrog.Core.Models;

public readonly record struct EyeOffset(
    double X,
    double Y)
{
    public static EyeOffset Centered { get; } =
        new(0, 0);
}
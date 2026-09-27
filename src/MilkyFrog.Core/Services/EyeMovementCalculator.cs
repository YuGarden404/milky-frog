using MilkyFrog.Core.Models;

namespace MilkyFrog.Core.Services;

public sealed class EyeMovementCalculator
{
    // 兼容旧测试和旧调用方。
    // 后续角色模型确定后，可以删除此方法。
    public EyeOffset Calculate(
        Point2D eyeCenter,
        Point2D target,
        double maximumHorizontalOffset,
        double maximumVerticalOffset)
    {
        ValidateLimits(
            maximumHorizontalOffset,
            maximumVerticalOffset);

        double deltaX = target.X - eyeCenter.X;
        double deltaY = target.Y - eyeCenter.Y;

        double distance = Math.Sqrt(
            (deltaX * deltaX) +
            (deltaY * deltaY));

        if (distance <= double.Epsilon)
        {
            return EyeOffset.Centered;
        }

        double directionX = deltaX / distance;
        double directionY = deltaY / distance;

        double ellipseScale = 1.0 / Math.Sqrt(
            (directionX * directionX) /
            (maximumHorizontalOffset *
             maximumHorizontalOffset) +
            (directionY * directionY) /
            (maximumVerticalOffset *
             maximumVerticalOffset));

        return new EyeOffset(
            directionX * ellipseScale,
            directionY * ellipseScale);
    }

    // 当前临时角色使用的共享视线算法。
    // 未来更换正式角色模型时可以整体替换。
    public EyeOffset CalculateShared(
        Point2D gazeCenter,
        Point2D target,
        double maximumHorizontalOffset,
        double maximumVerticalOffset,
        double responseDistance = 220)
    {
        ValidateLimits(
            maximumHorizontalOffset,
            maximumVerticalOffset);

        if (responseDistance <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(responseDistance));
        }

        double deltaX = target.X - gazeCenter.X;
        double deltaY = target.Y - gazeCenter.Y;

        double distance = Math.Sqrt(
            (deltaX * deltaX) +
            (deltaY * deltaY));

        if (distance <= double.Epsilon)
        {
            return EyeOffset.Centered;
        }

        double directionX = deltaX / distance;
        double directionY = deltaY / distance;

        double response = Math.Tanh(
            distance / responseDistance);

        return new EyeOffset(
            directionX *
            maximumHorizontalOffset *
            response,

            directionY *
            maximumVerticalOffset *
            response);
    }

    private static void ValidateLimits(
        double maximumHorizontalOffset,
        double maximumVerticalOffset)
    {
        if (maximumHorizontalOffset <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumHorizontalOffset));
        }

        if (maximumVerticalOffset <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumVerticalOffset));
        }
    }
}
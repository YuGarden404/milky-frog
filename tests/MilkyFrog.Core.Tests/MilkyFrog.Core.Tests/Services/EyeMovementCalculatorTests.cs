using MilkyFrog.Core.Models;
using MilkyFrog.Core.Services;

namespace MilkyFrog.Core.Tests.Services;

public sealed class EyeMovementCalculatorTests
{
    private readonly EyeMovementCalculator _calculator =
        new();

    [Fact]
    public void Calculate_TargetAtCenter_ReturnsCenteredOffset()
    {
        EyeOffset result = _calculator.Calculate(
            new Point2D(10, 20),
            new Point2D(10, 20),
            9,
            7);

        Assert.Equal(EyeOffset.Centered, result);
    }

    [Fact]
    public void Calculate_TargetToRight_ReachesHorizontalLimit()
    {
        EyeOffset result = _calculator.Calculate(
            new Point2D(0, 0),
            new Point2D(100, 0),
            9,
            7);

        Assert.Equal(9, result.X, precision: 6);
        Assert.Equal(0, result.Y, precision: 6);
    }

    [Fact]
    public void Calculate_TargetAbove_ReachesVerticalLimit()
    {
        EyeOffset result = _calculator.Calculate(
            new Point2D(0, 0),
            new Point2D(0, -100),
            9,
            7);

        Assert.Equal(0, result.X, precision: 6);
        Assert.Equal(-7, result.Y, precision: 6);
    }

    [Fact]
    public void Calculate_DiagonalTarget_StaysInsideEllipse()
    {
        const double horizontalLimit = 9;
        const double verticalLimit = 7;

        EyeOffset result = _calculator.Calculate(
            new Point2D(0, 0),
            new Point2D(100, 100),
            horizontalLimit,
            verticalLimit);

        double ellipseValue =
            (result.X * result.X) /
            (horizontalLimit * horizontalLimit) +
            (result.Y * result.Y) /
            (verticalLimit * verticalLimit);

        Assert.Equal(
            1,
            ellipseValue,
            precision: 6);
    }

    [Fact]
    public void CalculateShared_FarTarget_ApproachesHorizontalLimit()
    {
        EyeMovementCalculator calculator = new();

        EyeOffset result = calculator.CalculateShared(
            new Point2D(0, 0),
            new Point2D(5000, 0),
            6,
            5);

        Assert.InRange(result.X, 5.99, 6);
        Assert.Equal(0, result.Y, precision: 6);
    }

    [Fact]
    public void CalculateShared_ReturnsOneSharedOffset()
    {
        EyeMovementCalculator calculator = new();

        EyeOffset result = calculator.CalculateShared(
            new Point2D(100, 100),
            new Point2D(-1000, 100),
            6,
            5);

        Assert.True(result.X < 0);
        Assert.Equal(0, result.Y, precision: 6);
    }

    [Theory]
    [InlineData(0, 7)]
    [InlineData(-1, 7)]
    [InlineData(9, 0)]
    [InlineData(9, -1)]
    public void Calculate_InvalidLimit_Throws(
        double horizontalLimit,
        double verticalLimit)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _calculator.Calculate(
                new Point2D(0, 0),
                new Point2D(10, 10),
                horizontalLimit,
                verticalLimit));
    }
}
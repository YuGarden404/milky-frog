using MilkyFrog.Core.Settings;

namespace MilkyFrog.Core.Tests.Settings;

public sealed class AppSettingsTests
{
    [Fact]
    public void Defaults_AreValid()
    {
        var settings = new AppSettings();

        Assert.Equal(1, settings.Version);
        Assert.False(settings.IsMuted);
        Assert.Equal(1.0f, settings.Volume);
        Assert.Null(settings.WindowLeft);
        Assert.Null(settings.WindowTop);
    }

    [Theory]
    [InlineData(-0.5f, 0.0f)]
    [InlineData(0.4f, 0.4f)]
    [InlineData(1.5f, 1.0f)]
    public void Normalize_ClampsVolume(
        float input,
        float expected)
    {
        var settings = new AppSettings
        {
            Volume = input
        };

        AppSettings normalized =
            settings.Normalize();

        Assert.Equal(expected, normalized.Volume);
    }

    [Fact]
    public void Normalize_ReplacesNonFiniteVolume()
    {
        var settings = new AppSettings
        {
            Volume = float.NaN
        };

        AppSettings normalized =
            settings.Normalize();

        Assert.Equal(
            AppSettings.DefaultVolume,
            normalized.Volume);
    }

    [Fact]
    public void Normalize_ClearsIncompletePosition()
    {
        var settings = new AppSettings
        {
            WindowLeft = 120,
            WindowTop = null
        };

        AppSettings normalized =
            settings.Normalize();

        Assert.Null(normalized.WindowLeft);
        Assert.Null(normalized.WindowTop);
    }

    [Fact]
    public void Normalize_PreservesCompletePosition()
    {
        var settings = new AppSettings
        {
            WindowLeft = -800,
            WindowTop = 160
        };

        AppSettings normalized =
            settings.Normalize();

        Assert.Equal(-800, normalized.WindowLeft);
        Assert.Equal(160, normalized.WindowTop);
    }
}
using MilkyFrog.Core.Settings;

namespace MilkyFrog.Core.Abstractions;

public interface ISettingsService
{
    AppSettings Load();

    bool Save(AppSettings settings);
}
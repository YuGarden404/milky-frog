using System.IO;
using System.Windows.Media.Imaging;

namespace MilkyFrog.App.Animation;

public sealed class ImageAssetLoader
{
    private readonly Dictionary<
        string,
        BitmapImage?> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public BitmapImage? Load(string relativePath)
    {
        if (_cache.TryGetValue(
                relativePath,
                out BitmapImage? cached))
        {
            return cached;
        }

        string fullPath = Path.Combine(
            AppContext.BaseDirectory,
            relativePath.Replace(
                '/',
                Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath))
        {
            _cache[relativePath] = null;
            return null;
        }

        try
        {
            using var stream = File.OpenRead(fullPath);

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption =
                BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();

            _cache[relativePath] = image;
            return image;
        }
        catch (IOException)
        {
            _cache[relativePath] = null;
            return null;
        }

        catch (UnauthorizedAccessException)
        {
            _cache[relativePath] = null;
            return null;
        }
    }

    public void Clear()
    {
        _cache.Clear();
    }
}
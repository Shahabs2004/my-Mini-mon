using System.IO;
using System.Windows.Media.Imaging;

namespace SmartViewCompanion.Core;

public static class PhotoFrameLibrary
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff", ".wdp"
    };

    public static IReadOnlyList<string> GetImages(IEnumerable<string> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string source in sources.Where(s => !string.IsNullOrWhiteSpace(s)))
        {
            try
            {
                if (File.Exists(source) && SupportedExtensions.Contains(Path.GetExtension(source)))
                    files.Add(Path.GetFullPath(source));
                else if (Directory.Exists(source))
                {
                    foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
                    {
                        if (SupportedExtensions.Contains(Path.GetExtension(file)))
                            files.Add(Path.GetFullPath(file));
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }

        return files.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static BitmapImage Load(string path)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }
}

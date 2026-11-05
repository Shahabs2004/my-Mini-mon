using SmartViewCompanion.Core;

namespace SmartViewCompanion.Tests;

public sealed class PhotoFrameLibraryTests
{
    [Fact]
    public void GetImages_FindsSupportedFilesRecursively_AndIgnoresUnsupportedFiles()
    {
        string root = Path.Combine(Path.GetTempPath(), "SmartViewPhotoTests", Guid.NewGuid().ToString("N"));
        string nested = Path.Combine(root, "nested");
        Directory.CreateDirectory(nested);

        try
        {
            string jpg = Path.Combine(root, "one.jpg");
            string png = Path.Combine(nested, "two.png");
            string txt = Path.Combine(nested, "notes.txt");
            File.WriteAllBytes(jpg, []);
            File.WriteAllBytes(png, []);
            File.WriteAllText(txt, "not an image");

            var result = PhotoFrameLibrary.GetImages([root]);

            Assert.Equal(2, result.Count);
            Assert.Contains(Path.GetFullPath(jpg), result);
            Assert.Contains(Path.GetFullPath(png), result);
            Assert.DoesNotContain(Path.GetFullPath(txt), result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void GetImages_DeduplicatesSameFileSelectedAsFileAndFolder()
    {
        string root = Path.Combine(Path.GetTempPath(), "SmartViewPhotoTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string image = Path.Combine(root, "photo.jpeg");

        try
        {
            File.WriteAllBytes(image, []);
            var result = PhotoFrameLibrary.GetImages([root, image]);

            Assert.Single(result);
            Assert.Equal(Path.GetFullPath(image), result[0]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

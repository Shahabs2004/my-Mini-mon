using System.IO;
using System.Text.Json;

namespace SmartViewCompanion.Core;

public static class PhotoFrameSettingsStore
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SmartViewCompanion", "photo-frame.json");

    public static PhotoFrameSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new PhotoFrameSettings();

            return JsonSerializer.Deserialize<PhotoFrameSettings>(File.ReadAllText(SettingsPath))
                ?? new PhotoFrameSettings();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new PhotoFrameSettings();
        }
    }

    public static void Save(PhotoFrameSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        string? directory = Path.GetDirectoryName(SettingsPath);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        string temporaryPath = SettingsPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporaryPath, SettingsPath, true);
    }
}

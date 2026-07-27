using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace ChurchProjector.Classes;
public static class GlobalConfig
{
    private const string ConfigurationFileName = "Configuration.json";

    private static readonly string BundleConfigurationPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        ConfigurationFileName);

    private static readonly string MacOsAppDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChurchProjector");

    private static readonly string MacOsConfigurationPath = Path.Combine(MacOsAppDirectory, ConfigurationFileName);

    public static JsonFile JsonFile
    {
        get
        {
            if (field == null)
            {
                string? configurationFile = null;

                if (OperatingSystem.IsMacOS() && File.Exists(MacOsConfigurationPath))
                {
                    configurationFile = MacOsConfigurationPath;
                }
                else if (File.Exists(BundleConfigurationPath))
                {
                    configurationFile = BundleConfigurationPath;
                }

                if (configurationFile is not null)
                {
                    field = JsonSerializer.Deserialize(File.ReadAllText(configurationFile), typeof(JsonFile), JsonContext.Default) as JsonFile;
                    if (field == null)
                    {
                        throw new InvalidOperationException($"The json file could not be read.");
                    }
                }
                else
                {
                    field = new JsonFile();
                }
            }
            return field;
        }
    }

    public static void SaveChanges()
    {
        if (OperatingSystem.IsMacOS())
        {
            Directory.CreateDirectory(MacOsAppDirectory);
            File.WriteAllText(MacOsConfigurationPath, JsonSerializer.Serialize(JsonFile, JsonContext.Default.JsonFile));
            return;
        }

        File.WriteAllText(BundleConfigurationPath, JsonSerializer.Serialize(JsonFile, JsonContext.Default.JsonFile));
    }

    public static ObservableCollection<Song> Songs { get; } = [];
    public static ObservableCollection<Bible?> Bibles { get; } = [];
}

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace ChurchProjector.Classes;
public static class GlobalConfig
{
    public static JsonFile JsonFile
    {
        get
        {
            if (field == null)
            {
                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration.json")))
                {
                    field = JsonSerializer.Deserialize(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration.json")), typeof(JsonFile), JsonContext.Default) as JsonFile;
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
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration.json"), JsonSerializer.Serialize(JsonFile, JsonContext.Default.JsonFile));
    }

    public static ObservableCollection<Song> Songs { get; } = [];
    public static ObservableCollection<Bible?> Bibles { get; } = [];
}

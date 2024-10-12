using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace ChurchProjector.Classes;
public static class GlobalConfig
{
    private static JsonFile? _jsonFile;
    public static JsonFile JsonFile
    {
        get
        {
            if (_jsonFile == null)
            {
                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration.json")))
                {
                    _jsonFile = JsonSerializer.Deserialize(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration.json")), typeof(JsonFile), JsonContext.Default) as JsonFile;
                    if (_jsonFile == null)
                    {
                        throw new InvalidOperationException($"The json file could not be read.");
                    }
                }
                else
                {
                    _jsonFile = new JsonFile();
                }
            }
            return _jsonFile;
        }
    }

    public static void SaveChanges()
    {
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration.json"), JsonSerializer.Serialize(JsonFile, JsonContext.Default.JsonFile));
    }

    public static ObservableCollection<Song> Songs { get; } = [];
    public static ObservableCollection<Bible?> Bibles { get; } = [];
    public static HasError HasError { get; set; } = new();
}

public class HasError : ObservableObject
{
    private bool _value;
    public bool Value { get => _value; set => SetProperty(ref _value, value); }
}

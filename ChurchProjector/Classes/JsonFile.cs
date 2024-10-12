using ChurchProjector.Classes.Configuration.Image;
using ChurchProjector.Classes.Loaders;
using ChurchProjector.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ChurchProjector.Classes;

public class JsonFile : ObservableObject
{
    public Settings Settings { get; set; } = new Settings();
    public List<string> Schedules { get; set; } = [];
    public List<string> SelectedBibles { get; set; } = [];
}

public class Settings : ObservableObject
{
    public Theme Theme { get; set; }

    public OutputDisplaySettings OutputDisplaySettings { get; set; } = new();
    public DisplayConfiguration DisplayConfiguration { get; set; } = new();

    public BannerSettings BannerSettings { get; set; } = new();
    public PathSettings PathSettings { get; set; } = new();
    public BibleSettings BibleSettings { get; set; } = new();

    public string? SelectedMonitorName { get; set; }
}

public class DisplayConfiguration : ObservableObject
{
    private ImageCreationConfiguration _songConfiguration = new()
    {
        Header = new()
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            MinFontSize = 55,
            MaxFontSize = 65,
        },
        Content = new()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            MinFontSize = 55,
            MaxFontSize = 85,
        },
        Bottom = new()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            MinFontSize = 35,
            MaxFontSize = 40,
        },
    };
    public ImageCreationConfiguration SongConfiguration { get => _songConfiguration; set => SetProperty(ref _songConfiguration, value); }

    private ImageCreationConfiguration _bibleConfiguration = new()
    {
        Header = new()
        {
            MinFontSize = 65,
            MaxFontSize = 75,
            HorizontalAlignment = HorizontalAlignment.Center,
        },
        Content = new()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            MinFontSize = 75,
            MaxFontSize = 85,
        },
        Bottom = new()
        {
            MinFontSize = 40,
            MaxFontSize = 45,
            HorizontalAlignment = HorizontalAlignment.Right,
        },
    };
    public ImageCreationConfiguration BibleConfiguration { get => _bibleConfiguration; set => SetProperty(ref _bibleConfiguration, value); }
}

public partial class OutputDisplaySettings : ObservableObject
{
    public bool TopMost { get; set; } = true;
}

public class BannerSettings
{
    public int Speed { get; set; } = 20;
    public int TextSize { get; set; } = 20;
    public int Fps { get; set; } = 200;
}

public class PathSettings : ObservableObject
{
    private FileSystemWatcher? _biblesFileSystemWatcher = null;
    private string? _biblePath;
    public string? BiblePath
    {
        get => _biblePath;
        set
        {
            if (_biblePath != value)
            {
                _biblePath = value;
                OnPropertyChanged();
                _biblesFileSystemWatcher = CreateFileSystemWatcher(_biblesFileSystemWatcher, _biblePath, ["*.xml", "*.spb"], _bibleFileSystemWatcher_Changed, _bibleFileSystemWatcher_Changed);
                new Task(() => BibleLoader.LoadBibles(value, CancellationToken.None)).Start();
            }
        }
    }

    private void _bibleFileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
    {
        BibleLoader.LoadBibles(BiblePath, CancellationToken.None);
    }

    private FileSystemWatcher? _songsFileSystemWatcher = null;
    private string? _songPath;
    public string? SongPath
    {
        get => _songPath;
        set
        {
            if (_songPath != value)
            {
                _songPath = value;
                OnPropertyChanged();
                _songsFileSystemWatcher = CreateFileSystemWatcher(_songsFileSystemWatcher, _songPath, ["*.sng"], _songsFileSystemWatcher_Changed, _songsFileSystemWatcher_Changed);
                new Task(() => SongLoader.LoadSongs(value, CancellationToken.None)).Start();
            }
        }
    }

    private void _songsFileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
    {
        SongLoader.LoadSongs(SongPath, CancellationToken.None);
    }

    private static FileSystemWatcher? CreateFileSystemWatcher(FileSystemWatcher? currentFileSystemWatcher, string? path, List<string> filters, FileSystemEventHandler fileSystemEventHandler, RenamedEventHandler renamedEventArgs)
    {
        if (currentFileSystemWatcher is not null)
        {
            currentFileSystemWatcher.Changed -= fileSystemEventHandler;
            currentFileSystemWatcher.Created -= fileSystemEventHandler;
            currentFileSystemWatcher.Deleted -= fileSystemEventHandler;
            currentFileSystemWatcher.Renamed -= renamedEventArgs;
        }
        currentFileSystemWatcher?.Dispose();
        FileSystemWatcher? newFileSystemWatcher = null;
        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
        {
            newFileSystemWatcher = new(path)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            foreach (string filter in filters)
            {
                newFileSystemWatcher.Filters.Add(filter);
            }
            newFileSystemWatcher.Changed += fileSystemEventHandler;
            newFileSystemWatcher.Created += fileSystemEventHandler;
            newFileSystemWatcher.Deleted += fileSystemEventHandler;
            newFileSystemWatcher.Renamed += renamedEventArgs;
        }
        return newFileSystemWatcher;
    }
}

public class BibleSettings
{
    public BookLanguage BookLanguage { get; set; }
}
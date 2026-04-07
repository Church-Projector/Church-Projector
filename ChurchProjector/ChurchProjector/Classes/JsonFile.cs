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
    public Settings Settings { get; set; } = new();
    public List<string> Schedules { get; set; } = [];
    public List<string> SelectedBibles { get; set; } = [];
}

public class Settings : ObservableObject
{
    public Theme Theme { get; set; } = Theme.Light;
    public Language Language { get; set; } = Language.Windows;

    public bool ShowClock
    {
        get;
        set => SetProperty(ref field, value);
    }

    public OutputDisplaySettings OutputDisplaySettings { get; set; } = new();
    public DisplayConfiguration DisplayConfiguration { get; set; } = new();

    public BannerSettings BannerSettings { get; set; } = new();
    public PathSettings PathSettings { get; set; } = new();
    public BibleSettings BibleSettings { get; set; } = new();
    public SongSettings SongSettings { get; set; } = new();

    public string? SelectedMonitorName { get; set; }
}

public class DisplayConfiguration : ObservableObject
{
    public ImageCreationConfiguration SongConfiguration
    {
        get;
        set => SetProperty(ref field, value);
    } = new()
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

    public ImageCreationConfiguration BibleConfiguration
    {
        get;
        set => SetProperty(ref field, value);
    } = new()
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
}

public partial class OutputDisplaySettings : ObservableObject
{
    public bool TopMost { get; set; } = true;
}

public class BannerSettings
{
    public int Speed { get; set; } = 8;
    public int TextSize { get; set; } = 20;
    public int Fps { get; set; } = 200;
}

public class PathSettings : ObservableObject
{
    private FileSystemWatcher? _biblesFileSystemWatcher = null;

    public string? BiblePath
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                _biblesFileSystemWatcher = CreateFileSystemWatcher(_biblesFileSystemWatcher, field, ["*.xml", "*.spb"],
                    _bibleFileSystemWatcher_Changed, _bibleFileSystemWatcher_Changed);
                new Task(() => BibleLoader.LoadBibles(value, CancellationToken.None)).Start();
            }
        }
    }

    private void _bibleFileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
    {
        BibleLoader.LoadBibles(BiblePath, CancellationToken.None);
    }

    private FileSystemWatcher? _songsFileSystemWatcher = null;

    public string? SongPath
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                _songsFileSystemWatcher = CreateFileSystemWatcher(_songsFileSystemWatcher, field, ["*.sng"],
                    _songsFileSystemWatcher_Changed, _songsFileSystemWatcher_Changed);
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

public class SongSettings
{
    public bool ShowFirstLineOfNextSong { get; set; }
}
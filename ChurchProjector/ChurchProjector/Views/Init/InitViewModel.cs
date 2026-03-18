using ChurchProjector.Classes;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChurchProjector.Views.Init;
public partial class InitViewModel : ObservableObject
{
    public string? BiblesPath
    {
        get => GlobalConfig.JsonFile.Settings.PathSettings.BiblePath;
        set => SetProperty(BiblesPath, value, (newValue) => GlobalConfig.JsonFile.Settings.PathSettings.BiblePath = newValue);
    }

    public string? SongsPath
    {
        get => GlobalConfig.JsonFile.Settings.PathSettings.SongPath;
        set => SetProperty(SongsPath, value, (newValue) => GlobalConfig.JsonFile.Settings.PathSettings.SongPath = newValue);
    }
}

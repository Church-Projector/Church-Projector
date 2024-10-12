using ChurchProjector.Classes;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace ChurchProjector.Views.Song;
public class SongQuickSearchViewModel : ObservableObject
{
    private string _shortcut = string.Empty;
    public string Shortcut
    {
        get => _shortcut;
        set
        {
            if (_shortcut != value)
            {
                _shortcut = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasShortcut));
                OnPropertyChanged(nameof(MatchedSong));
            }
        }
    }
    public bool HasShortcut => !string.IsNullOrEmpty(MatchedSong);
    public string? MatchedSong
    {
        get
        {
            Classes.Song? song = GlobalConfig.Songs.ToList().Find(x => x.QuickFind?.Equals(Shortcut, System.StringComparison.OrdinalIgnoreCase) ?? false);
            if (song is null)
            {
                return null;
            }
            return $"Lied: {song.Title}";
        }
    }

    public required ICommand OpenSongCommand { get; init; }
    public required ICommand CloseDialogCommand { get; init; }
}

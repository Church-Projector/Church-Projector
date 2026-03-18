using ChurchProjector.Classes;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace ChurchProjector.Views.Song;
public class SongSearchViewModel : ObservableObject
{
    public List<Classes.Song> Songs => [.. GlobalConfig.Songs.ToList()
        .Where(x => _searchPartsLowerInvariant.All(y => x.Title.ToLowerInvariant().Contains(y)) || x.LowerContent.Contains(SearchText.ToLowerInvariant()))
        .OrderBy(x => !x.Title.Equals(SearchText, System.StringComparison.InvariantCultureIgnoreCase))
        .ThenBy(x => !x.Title.StartsWith(SearchText, System.StringComparison.InvariantCultureIgnoreCase))
        .ThenBy(x => !_searchPartsLowerInvariant.All(y => x.Title.ToLowerInvariant().Contains(y)))];

    public Classes.Song? SelectedSong
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSongSelected));
            }
        }
    }

    public string SearchText
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                _searchPartsLowerInvariant = value.ToLowerInvariant().Split(" ");
                OnPropertyChanged();
                OnPropertyChanged(nameof(Songs));
            }
        }
    } = string.Empty;

    private string[] _searchPartsLowerInvariant = [];

    public bool HasSongSelected => SelectedSong is not null;

    public required ICommand OpenSongCommand { get; init; }
    public required ICommand CloseDialogCommand { get; init; }
}

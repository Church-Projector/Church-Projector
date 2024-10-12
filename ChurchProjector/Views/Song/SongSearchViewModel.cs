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

    private Classes.Song? _selectedSong;
    public Classes.Song? SelectedSong
    {
        get => _selectedSong;
        set
        {
            if (_selectedSong != value)
            {
                _selectedSong = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSongSelected));
            }
        }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                _searchPartsLowerInvariant = value.ToLowerInvariant().Split(" ");
                OnPropertyChanged();
                OnPropertyChanged(nameof(Songs));
            }
        }
    }
    private string[] _searchPartsLowerInvariant = [];

    public bool HasSongSelected => SelectedSong is not null;

    public required ICommand OpenSongCommand { get; init; }
    public required ICommand CloseDialogCommand { get; init; }
}

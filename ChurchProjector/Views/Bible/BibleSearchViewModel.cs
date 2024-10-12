using ChurchProjector.Classes;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace ChurchProjector.Views.Bible;
public class BibleSearchViewModel : ObservableObject
{
    private string? _searchText;
    public string? SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Verses));
            }
        }
    }
    public ObservableCollection<Classes.Bible?> Bibles => GlobalConfig.Bibles;

    private Classes.Bible? _selectedBible;
    public Classes.Bible? SelectedBible
    {
        get => _selectedBible;
        set
        {
            if (_selectedBible != value)
            {
                _selectedBible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Verses));
            }
        }
    }

    public List<ExactVerse> Verses => SearchText is null ? [] : SelectedBible?.Books.SelectMany(b => b.Verses.Select(v => new ExactVerse(b.Title, b.Number, v.ChapterNumber, v.VerseNumber, v.Content)))
        .Where(x => x.Content.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
        .OrderBy(x => !x.Content.StartsWith(SearchText.Trim(), StringComparison.OrdinalIgnoreCase)).ToList() ?? [];

    private ExactVerse? _selectedVerse;
    public ExactVerse? SelectedVerse
    {
        get => _selectedVerse;
        set
        {
            if (_selectedVerse != value)
            {
                _selectedVerse = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasVerseSelected));
            }
        }
    }

    public bool HasVerseSelected => SelectedVerse is not null;

    public required ICommand OpenVerseCommand { get; init; }
    public required ICommand CloseDialogCommand { get; init; }
}

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
    public string? SearchText
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Verses));
            }
        }
    }

    public ObservableCollection<Classes.Bible?> Bibles => GlobalConfig.Bibles;

    public Classes.Bible? SelectedBible
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Verses));
            }
        }
    }

    public List<ExactVerse> Verses => SearchText is null ? [] : SelectedBible?.Books.SelectMany(b => b.Verses.Select(v => new ExactVerse(b.Title, b.Number, v.ChapterNumber, v.VerseNumber, v.Content)))
        .Where(x => x.Content.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
        .OrderBy(x => !x.Content.StartsWith(SearchText.Trim(), StringComparison.OrdinalIgnoreCase)).ToList() ?? [];

    public ExactVerse? SelectedVerse
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasVerseSelected));
            }
        }
    }

    public bool HasVerseSelected => SelectedVerse is not null;

    public required ICommand OpenVerseCommand { get; init; }
    public required ICommand CloseDialogCommand { get; init; }
}

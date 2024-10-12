using ChurchProjector.Classes;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace ChurchProjector.Views.Bible;
public partial class BibleViewModel : ObservableObject
{
    public BibleViewModel()
    {
        LoadBibles();
        GlobalConfig.Bibles.CollectionChanged += Bibles_CollectionChanged;
        this.PropertyChanged += BibleViewModel_PropertyChanged;
    }

    private void BibleViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SelectedBiblePosition) or nameof(SearchHint))
        {
            OnPropertyChanged(nameof(BibleSearchHintPreview));
        }
    }

    private void Bibles_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        LoadBibles();
    }

    private void LoadBibles()
    {
        SelectedBible1 = Bibles.FirstOrDefault(x => x?.Filename == GlobalConfig.JsonFile.SelectedBibles.FirstOrDefault());
        OnPropertyChanged(nameof(SelectedBible1));
        SelectedBible2 = Bibles.FirstOrDefault(x => x?.Filename == GlobalConfig.JsonFile.SelectedBibles.Skip(1).FirstOrDefault());
        OnPropertyChanged(nameof(SelectedBible2));
        SelectedBible3 = Bibles.FirstOrDefault(x => x?.Filename == GlobalConfig.JsonFile.SelectedBibles.Skip(2).FirstOrDefault());
        OnPropertyChanged(nameof(SelectedBible3));
    }

    public ObservableCollection<Classes.Bible?> Bibles => GlobalConfig.Bibles;
    private Classes.Bible? _selectedBible1;
    public Classes.Bible? SelectedBible1
    {
        get => _selectedBible1;
        set
        {
            if (_selectedBible1 != value)
            {
                _selectedBible1 = value;
                OnPropertyChanged();
                SetBiblePosition(SelectedBookPart, SelectedChapter, SelectedVerseStart, SelectedVerseEnd);
                if (value is null || value.Filename != GlobalConfig.JsonFile.SelectedBibles.FirstOrDefault())
                {
                    UpdateConfiguredBibles();
                }
            }
        }
    }

    private Classes.Bible? _selectedBible2;
    public Classes.Bible? SelectedBible2
    {
        get => _selectedBible2;
        set
        {
            if (_selectedBible2 != value)
            {
                _selectedBible2 = value;
                if (value is null || value.Filename != GlobalConfig.JsonFile.SelectedBibles.Skip(1).FirstOrDefault())
                {
                    UpdateConfiguredBibles();
                }
            }
        }
    }

    private Classes.Bible? _selectedBible3;
    public Classes.Bible? SelectedBible3
    {
        get => _selectedBible3;
        set
        {
            if (_selectedBible3 != value)
            {
                _selectedBible3 = value;
                if (value is null || value.Filename != GlobalConfig.JsonFile.SelectedBibles.Skip(2).FirstOrDefault())
                {
                    UpdateConfiguredBibles();
                }
            }
        }
    }

    private void UpdateConfiguredBibles()
    {
        GlobalConfig.JsonFile.SelectedBibles = new List<string?>()
                {
                    SelectedBible1?.Filename,
                    SelectedBible2?.Filename,
                    SelectedBible3?.Filename
                }.Where(x => !string.IsNullOrEmpty(x))
        .Cast<string>()
        .ToList();
    }

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

                if (SelectedBible1 is null || string.IsNullOrWhiteSpace(SearchText))
                {
                    SetBiblePosition(bookName: null, chapter: null, verseStart: null, verseEnd: null);
                    return;
                }
                Match match = BiblePositionRegex().Match(SearchText);
                SetBiblePosition(bookName: $"{match.Groups[1]} {match.Groups[2]}".Trim(),
                                 chapter: match.Groups[3].Success && int.TryParse(match.Groups[3].Value, out int chapter) ? chapter : null,
                                 verseStart: match.Groups[4].Success && int.TryParse(match.Groups[4].Value, out int verseStart) ? verseStart : null,
                                 verseEnd: match.Groups[5].Success && int.TryParse(match.Groups[5].Value, out int verseEnd) ? verseEnd : null);
            }
        }
    }

    internal void SetBiblePosition(string? bookName, int? chapter, int? verseStart, int? verseEnd)
    {
        Error = null;
        string GetChars(string text) => string.Concat(text.Where(c => char.IsLetter(c) || char.IsDigit(c)));
        // Removing .
        // Some translation have "1 Mose" and some "1. Mose"
        Book? book = string.IsNullOrEmpty(bookName)
            ? null
            : SelectedBible1?.Books.FirstOrDefault(x => GetChars(x.SearchTitle).StartsWith(GetChars(bookName), StringComparison.OrdinalIgnoreCase));
        SelectedBookPart = bookName;
        SelectedBookName = book?.Title;
        SelectedChapter = book is not null ? chapter : null;
        SelectedVerseStart = SelectedChapter.HasValue ? verseStart : null;
        SelectedVerseEnd = SelectedVerseStart.HasValue ? verseEnd ?? verseStart : null;

        SelectedBiblePosition = GetSelectedBiblePositionHeader(SelectedBible1, book?.Number, chapter, verseStart, verseEnd, searchTitle: true);

        SearchHint = book is not null || string.IsNullOrWhiteSpace(SearchText) ? null : $"Nach der Bibelstelle: {SearchText} suchen.";

        if (book is not null)
        {
            if (SelectedChapter.HasValue && chapter > book.Verses.Max(x => x.ChapterNumber))
            {
                Error = $"Das Buch \"{SelectedBookName}\" besitzt keine {SelectedChapter} Kapitel.";
            }
            else if (SelectedChapter.HasValue && SelectedChapter <= 0)
            {
                Error = "Das Kapitel muss größer als 0 sein.";
            }
            else if (SelectedChapter.HasValue && SelectedVerseStart.HasValue && SelectedVerseStart <= 0)
            {
                Error = "Der Vers muss größer als 0 sein.";
            }
            else if (SelectedChapter.HasValue && SelectedVerseStart.HasValue && SelectedVerseStart > book.Verses.Where(x => x.ChapterNumber == SelectedChapter.Value).Max(x => x.VerseNumber))
            {
                Error = $"Das Kapitel {SelectedChapter} in \"{SelectedBookName}\" besitzt keine {SelectedVerseStart} Verse.";
            }
            else if (SelectedChapter.HasValue && SelectedVerseEnd.HasValue && SelectedVerseEnd <= 0)
            {
                Error = "Der Vers muss größer als 0 sein.";
            }
            else if (SelectedChapter.HasValue && SelectedVerseEnd.HasValue && SelectedVerseEnd > book.Verses.Where(x => x.ChapterNumber == SelectedChapter.Value).Max(x => x.VerseNumber))
            {
                Error = $"Das Kapitel {SelectedChapter} in \"{SelectedBookName}\" besitzt keine {SelectedVerseEnd} Verse.";
            }
            else if (SelectedVerseStart.HasValue && SelectedVerseEnd.HasValue && SelectedVerseStart.Value > SelectedVerseEnd.Value)
            {
                Error = "Der Vers von kann nicht größer sein als der Vers bis.";
            }
            else
            {
                Error = null;
            }
        }

        OnPropertyChanged(nameof(MayAcceptSearchTextCommand));
        OnPropertyChanged(nameof(PreviewSelectedBiblePosition));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoBack));
    }

    internal string? GetSelectedBiblePositionHeader(Classes.Bible? bible, int? bookNumber, int? chapter, int? verseStart, int? verseEnd, bool searchTitle = false)
    {
        if (SelectedBible1 is null || bible is null || !bookNumber.HasValue)
        {
            return null;
        }
        Book globalBook = SelectedBible1.Books.Single(x => x.Number == bookNumber);
        int? globalChapter = chapter is null ? null : (globalBook.Verses.FirstOrDefault(x => x.ChapterNumber == chapter)?.GlobalChapterNumber ?? chapter);
        int? globalVerseStart = verseStart is null ? null : globalBook.Verses.FirstOrDefault(x => x.ChapterNumber == chapter && x.VerseNumber == verseStart)?.GlobalVerseNumber ?? verseStart;
        int? globalVerseEnd = verseEnd is null ? globalVerseStart : (globalBook.Verses.FirstOrDefault(x => x.ChapterNumber == chapter && x.VerseNumber == verseEnd)?.GlobalVerseNumber ?? verseEnd);

        Book? book = bible?.Books.FirstOrDefault(b => b.Number == bookNumber);
        if (bookNumber.HasValue && book is not null)
        {
            StringBuilder sb = new(searchTitle ? book.SearchTitle : book.Title);
            if (globalChapter.HasValue)
            {
                sb.Append($" {book.Verses.FirstOrDefault(x => x.GlobalChapterNumber == globalChapter)?.ChapterNumber ?? chapter}");
            }
            if (globalVerseStart.HasValue)
            {
                sb.Append($", {book.Verses.FirstOrDefault(x => x.GlobalChapterNumber == globalChapter && x.GlobalVerseNumber == globalVerseStart)?.VerseNumber ?? verseStart}");
                if (globalVerseStart != globalVerseEnd && globalVerseEnd.HasValue)
                {
                    sb.Append($" - {book.Verses.FirstOrDefault(x => x.GlobalChapterNumber == globalChapter && x.GlobalVerseNumber == globalVerseEnd)?.VerseNumber ?? verseEnd}");
                }
            }
            return sb.ToString();
        }
        return null;
    }

    [ObservableProperty]
    private string? _selectedBookPart;

    [ObservableProperty]
    private string? _selectedBookName;

    public int SelectedBookNumber => SelectedBible1?.Books.First(b => b.Title == SelectedBookName).Number ?? throw new InvalidOperationException("No bible is selected.");

    [ObservableProperty]
    private int? _selectedChapter;

    [ObservableProperty]
    private int? _selectedVerseStart;

    [ObservableProperty]
    private int? _selectedVerseEnd;

    public bool CanGoBack => SelectedBible1 is not null
                && SelectedBookName is not null
                && SelectedBible1.Books.Any(b => b.Title == SelectedBookName)
                && SelectedChapter is not null
                && SelectedChapter > 0
                && SelectedChapter.Value <= SelectedBible1.Books.First(b => b.Title == SelectedBookName).Verses.Max(x => x.ChapterNumber)
                && SelectedVerseStart is not null
                && SelectedVerseStart.Value > 1
                && string.IsNullOrEmpty(Error);

    public bool CanGoNext => SelectedBible1 is not null
                && SelectedBookName is not null
                && SelectedBible1.Books.Any(b => b.Title == SelectedBookName)
                && SelectedChapter is not null
                && SelectedChapter > 0
                && SelectedChapter.Value <= SelectedBible1.Books.First(b => b.Title == SelectedBookName).Verses.Max(x => x.ChapterNumber)
                && SelectedVerseEnd is not null
                && SelectedVerseEnd.Value < SelectedBible1.Books.First(b => b.Title == SelectedBookName).Verses.Where(x => x.ChapterNumber == SelectedChapter.Value).Max(x => x.VerseNumber)
                && string.IsNullOrEmpty(Error);

    public bool MayAcceptSearchTextCommand => (SelectedVerseStart.HasValue || !string.IsNullOrWhiteSpace(SearchHint)) && string.IsNullOrEmpty(Error);

    [ObservableProperty]
    private string? _selectedBiblePosition = null;

    [ObservableProperty]
    private string? _searchHint = null;

    public string? BibleSearchHintPreview => string.IsNullOrEmpty(SelectedBiblePosition) ? SearchHint : SelectedBiblePosition;

    public Dictionary<int, string> PreviewSelectedBiblePosition
    {
        get
        {
            if (SelectedBible1 is null
                || SelectedBookName is null
                || !SelectedBible1.Books.Any(b => b.Title == SelectedBookName)
                || SelectedChapter is null
                || SelectedChapter <= 0
                || SelectedChapter.Value > SelectedBible1.Books.First(b => b.Title == SelectedBookName).Verses.Max(x => x.ChapterNumber)
                || SelectedVerseStart <= 0
                || SelectedVerseStart > SelectedBible1.Books.First(b => b.Title == SelectedBookName).Verses.Where(x => x.ChapterNumber == SelectedChapter).Max(x => x.VerseNumber)
                || SelectedVerseEnd <= 0
                || SelectedVerseEnd > SelectedBible1.Books.First(b => b.Title == SelectedBookName).Verses.Where(x => x.ChapterNumber == SelectedChapter).Max(x => x.VerseNumber))
            {
                return [];
            }
            return GetPreviewSelectedBiblePositionInternal(SelectedBible1, SelectedBookNumber, SelectedChapter.Value, SelectedVerseStart, SelectedVerseEnd, withUpper: false, forceAllVerses: true);
        }
    }

    public List<string> GetPreviewSelectedBiblePosition(Classes.Bible bible, int bookNumber, int chapter, int? verseStart, int? verseEnd)
    {
        Dictionary<int, string> verses = GetPreviewSelectedBiblePositionInternal(bible, bookNumber, chapter, verseStart, verseEnd, withUpper: true);

        if (verses.Count == 1)
        {
            return [verses.Values.First()];
        }
        List<string> verseContent = [];
        foreach (KeyValuePair<int, string> verse in verses)
        {
            verseContent.Add($"<upper>{verse.Key}</upper> {verse.Value}");
        }
        return verseContent;
    }

    public Dictionary<int, string> GetPreviewSelectedBiblePositionInternal(Classes.Bible bible, int bookNumber, int chapter, int? verseStart, int? verseEnd, bool withUpper = false, bool forceAllVerses = false)
    {
        if (SelectedBible1 is null)
        {
            return [];
        }
        Book globalBook = SelectedBible1.Books.Single(x => x.Number == bookNumber);
        int globalChapter = globalBook.Verses.First(x => x.ChapterNumber == chapter).GlobalChapterNumber;
        int? globalVerseStart = verseStart is null ? null : globalBook.Verses.First(x => x.ChapterNumber == chapter && x.VerseNumber == verseStart).GlobalVerseNumber;
        int? globalVerseEnd = verseEnd is null ? globalVerseStart : globalBook.Verses.First(x => x.ChapterNumber == chapter && x.VerseNumber == verseEnd).GlobalVerseNumber;

        List<BibleVerse> allVerses = bible.Books.First(b => b.Number == bookNumber).Verses.Where(x => x.GlobalChapterNumber == globalChapter).ToList();
        List<BibleVerse> verses;
        if (!forceAllVerses && globalVerseStart.HasValue)
        {
            verses = [];
            for (int i = globalVerseStart.Value; i <= (globalVerseEnd ?? globalVerseStart).Value; i++)
            {
                verses.AddRange(allVerses.Where(x => x.GlobalVerseNumber == i));
            }
        }
        else
        {
            verses = allVerses;
        }
        return verses.ToDictionary(x => x.VerseNumber, x => x.Content);
    }


    private string? _error;
    public string? Error
    {
        get => _error;
        set
        {
            if (_error != value)
            {
                _error = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(MayAcceptSearchTextCommand));
            }
        }
    }
    public bool HasError => !string.IsNullOrEmpty(Error);

    public required ICommand AcceptSearchTextCommand { get; set; }
    public required ICommand BackVerseCommand { get; set; }
    public required ICommand NextVerseCommand { get; set; }

    [GeneratedRegex(@"^(\d*)[\s,.]*([\p{L}]*)[\s,.]*(\d*)[\s,.]*(\d*)[\s,.]*-*[\s,.]*(\d*)")]
    private static partial Regex BiblePositionRegex();
}

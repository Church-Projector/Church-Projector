using ChurchProjector.Classes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace ChurchProjector.Views.Song;
public partial class SongEditViewModel : ObservableObject
{
    public required Classes.Song Song { get; init; }
    public bool MaySave { get; set; } = true;

    public required ICommand SaveDialogCommand { get; init; }
    public required ICommand CloseDialogCommand { get; init; }

    [RelayCommand]
    private void AddVerse()
    {
        Song.Verses.Add(new());
    }

    [RelayCommand]
    private void RemoveVerse(Verse verse)
    {
        Song.Verses.Remove(verse);
    }
}

using ChurchProjector.Classes.History;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ChurchProjector.Views.History;
public partial class HistoryViewModel : ObservableObject
{
    public ObservableCollection<HistoryEntry> Histories { get; set; } = [];

    public ICommand? OpenHistoryEntryCommand
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ICommand? RemoveHistoryEntryCommand
    {
        get;
        set => SetProperty(ref field, value);
    }

    [RelayCommand]
    private void ClearHistory()
    {
        Histories.Clear();
    }
}

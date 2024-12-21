using ChurchProjector.Classes.History;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ChurchProjector.Views.History;
public partial class HistoryViewModel : ObservableObject
{
    public ObservableCollection<HistoryEntry> Histories { get; set; } = [];
    private ICommand? _openHistoryEntryCommand;
    public ICommand? OpenHistoryEntryCommand { get => _openHistoryEntryCommand; set => SetProperty(ref _openHistoryEntryCommand, value); }
    private ICommand? _removeHistoryEntryCommand;
    public ICommand? RemoveHistoryEntryCommand { get => _removeHistoryEntryCommand; set => SetProperty(ref _removeHistoryEntryCommand, value); }
    [RelayCommand]
    private void ClearHistory()
    {
        Histories.Clear();
    }
}

using Avalonia;
using Avalonia.Controls;
using ChurchProjector.Classes;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChurchProjector.Views.Song;
public partial class SongQuickSearch : Window
{
    private readonly SongQuickSearchViewModel _viewModel;

    public SongQuickSearch()
    {
        InitializeComponent();

        _viewModel = new SongQuickSearchViewModel()
        {
            CloseDialogCommand = new RelayCommand(Close),
            OpenSongCommand = new RelayCommand(() =>
            {
                if (_viewModel is null)
                {
                    return;
                }
                Classes.Song? selectedSong = GlobalConfig.Songs.FirstOrDefault(x => x.QuickFind?.Equals(_viewModel.Shortcut, StringComparison.OrdinalIgnoreCase) ?? false);
                if (selectedSong is null)
                {
                    return;
                }

                OpenSong?.Invoke(selectedSong.Title, selectedSong.GetImages().ConvertAll(x => new ImageWithName(x.title, x.bitmap, x.isOverflowing)), selectedSong.FilePath);
                Close();
            }),
        };
        DataContext = _viewModel;
        TxtShortcut.AttachedToVisualTree += TxtShortcut_AttachedToVisualTree;
    }

    public required Action<string, List<ImageWithName>, string> OpenSong { get; set; }

    // https://github.com/AvaloniaUI/Avalonia/issues/4835#issuecomment-706530463
    private void TxtShortcut_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        TxtShortcut.Focus();
    }
}

using Avalonia;
using Avalonia.Controls;
using ChurchProjector.Classes;
using CommunityToolkit.Mvvm.Input;
using System;

namespace ChurchProjector.Views.Bible;
public partial class BibleSearchWindow : Window
{
    private readonly BibleSearchViewModel _viewModel;
    public BibleSearchWindow()
    {
        InitializeComponent();
        _viewModel = null!;
    }

    public BibleSearchWindow(Classes.Bible bible, string searchText) : this()
    {
        DataContext = _viewModel;

        _viewModel = new BibleSearchViewModel()
        {
            SelectedBible = bible,
            SearchText = searchText,
            CloseDialogCommand = new RelayCommand(Close),
            OpenVerseCommand = new RelayCommand(() =>
            {
                if (_viewModel is null || _viewModel.SelectedVerse is null)
                {
                    return;
                }
                OpenVerse?.Invoke(_viewModel.SelectedVerse);
                Close();
            }),
        };
        DataContext = _viewModel;
        TxtSearchBox.AttachedToVisualTree += TxtShortcut_AttachedToVisualTree;
    }

    public required Action<ExactVerse> OpenVerse { get; set; }

    // https://github.com/AvaloniaUI/Avalonia/issues/4835#issuecomment-706530463
    private void TxtShortcut_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        TxtSearchBox.Focus();
    }

    private void ListBox_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (e.Source is StyledElement se && se.DataContext is ExactVerse)
        {
            _viewModel.OpenVerseCommand.Execute(null);
        }
    }
}

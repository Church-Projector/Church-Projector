using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ChurchProjector.Classes;
using System.Collections.Generic;

namespace ChurchProjector.Views.Init;

public partial class InitWindow : Window
{
    private readonly InitViewModel _viewModel = new();
    public InitWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    private async void OnBtnSelectBiblesPathClick(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFolder> result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
        if (result.Count == 1)
        {
            _viewModel.BiblesPath = result[0].Path.LocalPath;
        }
    }

    private async void OnBtnSelectSongsPathClick(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFolder> result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
        if (result.Count == 1)
        {
            _viewModel.SongsPath = result[0].Path.LocalPath;
        }
    }

    private void BtnOpenLink_Click(object? sender, RoutedEventArgs e)
    {
        string? url = ((Button)sender).Tag?.ToString();
        Link.OpenLink(url);
    }

    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
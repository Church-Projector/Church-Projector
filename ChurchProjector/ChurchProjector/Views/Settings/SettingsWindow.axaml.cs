using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ChurchProjector.Classes;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ChurchProjector.Views.Settings;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow()
    {
        _viewModel = null!;
        InitializeComponent();
    }

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private void OnBtnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void OnBtnSelectBiblesPathClick(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFolder> result =
            await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
        if (result.Count == 1)
        {
            _viewModel.BiblesPath = result[0].Path.LocalPath;
        }
    }

    private async void OnBtnSelectSongsPathClick(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFolder> result =
            await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
        if (result.Count == 1)
        {
            _viewModel.SongsPath = result[0].Path.LocalPath;
        }
    }

    private void BtnOpenLink_Click(object? sender, RoutedEventArgs e)
    {
        string? url = ((Button)sender).Tag?.ToString();
        if (url.StartsWith("http"))
        {
            Link.OpenLink(url);
        }
        else
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("explorer.exe", url);
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = url,
                    UseShellExecute = false
                });
            }
        }
    }
}
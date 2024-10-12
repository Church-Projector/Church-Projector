using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }
        try
        {
            Process.Start(url);
        }
        catch
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }
}

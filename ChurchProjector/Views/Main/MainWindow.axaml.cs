using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.VisualTree;
using ChurchProjector.Classes;
using ChurchProjector.Views.Bible;
using ChurchProjector.Views.Notification;
using ChurchProjector.Views.Settings;
using ChurchProjector.Views.Song;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace ChurchProjector.Views.Main;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public MainWindow()
    {
        _viewModel = null!;
        InitializeComponent();
    }

    public MainWindow(SettingsViewModel settings) : this()
    {
        string? version = ChurchProjector.Classes.Version.GetCurrentVersion();

        Title = $"{Title} {version}";

        List<Screen> screens = [.. Screens.All];

        settings.SetMonitors(GetMonitors());

        DataContext = _viewModel = new MainViewModel(this, settings, StorageProvider, Clipboard, version);

        _viewModel.SetHasSecondScreen(Screens.ScreenCount > 1);

        _viewModel.PropertyChanged += _viewModel_PropertyChanged;

        _viewModel.OpenSongQuickSearchCommand = new RelayCommand(() => new SongQuickSearch() { OpenSong = _viewModel.RenderImages, }.ShowDialog(this));
        _viewModel.OpenSongSearchCommand = new RelayCommand(() => new SongSearch() { OpenSong = _viewModel.RenderImages, }.ShowDialog(this));
        _viewModel.OpenBibleSearchCommand = new RelayCommand(() => new BibleSearchWindow(_viewModel.BibleViewModel.SelectedBible1, string.Empty) { OpenVerse = _viewModel.OpenVerse, }.ShowDialog(this));
        _viewModel.HideImageCommand = new RelayCommand<bool>((bool fadeOut) => _viewModel.HideImage(fadeOut));
        _viewModel.OpenShowNotificationCommand = new RelayCommand(async () =>
        {
            if (string.IsNullOrWhiteSpace(_viewModel.ImageWindow.ViewModel.BannerText))
            {
                await new NotificationWindow() { ShowNotification = (string value) => _viewModel.ImageWindow.ViewModel.BannerText = value }.ShowDialog(this);
                _viewModel.ImageWindow.CreateBanner();
            }
            else
            {
                _viewModel.ImageWindow.ViewModel.BannerText = string.Empty;
                _viewModel.ImageWindow.StopBanner();
            }
        });
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)){
            _viewModel.OpenLogsCommand = new RelayCommand(() => Process.Start("explorer.exe", Path.Combine(AppContext.BaseDirectory, "logs")));
        } else {
            // todo
        }
        _viewModel.EditCommand = new RelayCommand(() => new SongEditWindow(_viewModel.Images!.Filename!).ShowDialog(this));
        _viewModel.AddSongCommand = new RelayCommand(() => new SongEditWindow().ShowDialog(this));

        Screens.Changed += Screens_Changed;
    }

    private void Screens_Changed(object? sender, EventArgs e)
    {
        _viewModel.Settings.SetMonitors(GetMonitors());
        _viewModel.SetHasSecondScreen(Screens.ScreenCount > 1);
    }

    private Dictionary<string, Screen> GetMonitors()
    {
        List<Screen> screens = [.. Screens.All];

        return screens.ToDictionary(x => x.IsPrimary
            ? $"Hauptbildschirm{(string.IsNullOrWhiteSpace(x.DisplayName) ? "" : $" ({x.DisplayName})")}"
            : $"{screens.Where(y => !y.IsPrimary).ToList().IndexOf(x) + 2}. Bildschirm{(string.IsNullOrWhiteSpace(x.DisplayName) ? "" : $" ({x.DisplayName})")}", x => x); ;
    }

    private void _viewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Images))
        {
            Control? firstFocusableItem = LboImages.GetVisualDescendants().OfType<Control>().FirstOrDefault(x => x.Focusable);
            if (firstFocusableItem is not null)
            {
                firstFocusableItem.Focus();
            }
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _cancellationTokenSource.Cancel();
        GlobalConfig.SaveChanges();
        _viewModel.StopAll();
        Environment.Exit(0);
    }

    private void OnBtnSettingsClick(object sender, RoutedEventArgs e)
    {
        SettingsWindow settingsWindow = new(_viewModel.Settings);
        settingsWindow.Show(this);
    }

    private void ListBox_KeyUp(object? sender, KeyEventArgs e)
    {
        try
        {
            if (_viewModel.Images is null || _viewModel.Images.Images.Count == 0 || _viewModel.SelectedImage is null)
            {
                return;
            }
            double width = LboImages.Bounds.Width;
            var listBoxItems = LboImages.GetVisualDescendants().OfType<ListBoxItem>().ToList();
            if (listBoxItems.Count == 0)
            {
                return;
            }
            double listBoxItemWidth = listBoxItems.First().Bounds.Width;

            int columnsCount = (int)(width / listBoxItemWidth);

            int selectedImage = _viewModel.Images.Images.IndexOf(_viewModel.SelectedImage);

            if (e.Key == Key.Down)
            {
                selectedImage = Math.Min(_viewModel.Images.Images.Count - 1, selectedImage + columnsCount);
            }
            else if (e.Key == Key.Up)
            {
                selectedImage = Math.Max(0, selectedImage - columnsCount);
            }
            //else if (e.Key == Key.Enter)
            //{
            //    selectedImage = Math.Min(_viewModel.Images.Images.Count - 1, selectedImage + 1);
            //}

            _viewModel.SelectedImage = _viewModel.Images.Images[selectedImage];
            listBoxItems[selectedImage].Focus();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler bei dem KeyUp");
        }
    }
}
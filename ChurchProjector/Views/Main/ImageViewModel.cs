using Avalonia.Media;
using Avalonia.Threading;
using ChurchProjector.Classes;
using ChurchProjector.Views.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Linq;
using System.Threading;

namespace ChurchProjector.Views.Main;

public partial class ImageViewModel : ObservableObject
{
    public ImageViewModel(SettingsViewModel settings)
    {
        Settings = settings;
    }

    public Action? MediaEnded;

    private CancellationTokenSource? _cancellationTokenSource = null;
    private IImage? _imageSource;
    public IImage? ImageSource
    {
        get => _imageSource;
        set
        {
            _cancellationTokenSource?.Cancel();
            Opacity = 1;
            SetProperty(ref _imageSource, value);
        }
    }

    [ObservableProperty]
    private double _opacity = 1;

    public void HideImage(bool fadeOut)
    {
        _cancellationTokenSource?.Cancel();
        if (fadeOut)
        {
            _cancellationTokenSource = new();
            CancellationTokenSource cts = _cancellationTokenSource;
            DispatcherTimer.Run(() =>
            {
                if (cts.IsCancellationRequested)
                {
                    _cancellationTokenSource = null;
                    return false;
                }
                Opacity -= 0.05;
                if (Opacity > 0)
                {
                    return true;
                }
                ImageSource = null;
                cts.Dispose();
                _cancellationTokenSource = null;
                return false;
            }, TimeSpan.FromSeconds(0.1));
        }
        else
        {
            ImageSource = null;
        }

    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowVideo))]
    [NotifyPropertyChangedFor(nameof(ShowImage))]
    private FileType? _currentFileType;
    public bool ShowVideo => CurrentFileType == FileType.Movie;
    public bool ShowImage => CurrentFileType == FileType.Image;

    [ObservableProperty]
    private bool _bannerVisible;

    private string? _bannerText = null;
    public string? BannerText
    {
        get => string.IsNullOrWhiteSpace(_bannerText) ? null : string.Concat(Enumerable.Range(0, 20).Select(x => $"{_bannerText.Trim()} +++ ")).Trim();
        set => SetProperty(ref _bannerText, value);
    }

    public SettingsViewModel Settings { get; set; }
}

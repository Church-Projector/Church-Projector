using Avalonia.Media;
using Avalonia.Threading;
using ChurchProjector.Classes;
using ChurchProjector.Views.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChurchProjector.Views.Main;

public partial class ImageViewModel : ObservableObject
{
    private readonly DispatcherTimer _dispatcherTimer;
    public ImageViewModel(SettingsViewModel settings)
    {
        Settings = settings;
        _dispatcherTimer = new DispatcherTimer()
        {
            Interval = new TimeSpan(0, 1, 0),
        };
        _dispatcherTimer.Tick += DispatcherTimer_Tick;
        ClockText = DateTime.Now.ToString("HH:mm");
        StartTimerAtMinuteChangeAsync();

        IsClockVisible = GlobalConfig.JsonFile.Settings.ShowClock;
        GlobalConfig.JsonFile.Settings.PropertyChanged += Settings_PropertyChanged;
    }

    private void Settings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GlobalConfig.JsonFile.Settings.ShowClock))
        {
            IsClockVisible = GlobalConfig.JsonFile.Settings.ShowClock;
        }
    }

    private async Task StartTimerAtMinuteChangeAsync()
    {
        DateTime now = DateTime.Now;
        DateTime inOneMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(1);
        TimeSpan delay = inOneMinute - now;
        await Task.Delay(delay);
        _dispatcherTimer.IsEnabled = true;
        _dispatcherTimer.Start();
        ClockText = DateTime.Now.ToString("HH:mm");
    }

    private void DispatcherTimer_Tick(object? sender, EventArgs e)
    {
        ClockText = DateTime.Now.ToString("HH:mm");
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

    private bool _isBannerVisible;
    public bool IsBannerVisible
    {
        get => _isBannerVisible;
        set
        {
            if (SetProperty(ref _isBannerVisible, value))
            {
                this.OnPropertyChanged(nameof(ShowBottomBar));
            }
        }
    }
    private bool _isClockVisible;
    public bool IsClockVisible
    {
        get => _isClockVisible;
        set
        {
            if (SetProperty(ref _isClockVisible, value))
            {
                this.OnPropertyChanged(nameof(ShowBottomBar));
            }
        }
    }
    public bool ShowBottomBar => _isBannerVisible || _isClockVisible;

    private string? _bannerText = null;
    public string? BannerText
    {
        get => string.IsNullOrWhiteSpace(_bannerText) ? null : string.Concat(Enumerable.Range(0, 20).Select(x => $"{_bannerText.Trim()} +++ ")).Trim();
        set => SetProperty(ref _bannerText, value);
    }
    private string? _clockText;
    public string? ClockText
    {
        get => _clockText;
        set => SetProperty(ref _clockText, value);
    }

    public SettingsViewModel Settings { get; set; }
    public double TextSize => GlobalConfig.JsonFile.Settings.BannerSettings.TextSize;
}

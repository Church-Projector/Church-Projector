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
    private static readonly IBrush WarningBrush = new SolidColorBrush(Color.FromRgb(208, 135, 0));
    private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.FromRgb(233, 0, 62));

    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _countdownTimer;
    private DateTimeOffset _countdownEndsAt;

    public ImageViewModel(SettingsViewModel settings)
    {
        Settings = settings;
        _clockTimer = new DispatcherTimer()
        {
            Interval = new TimeSpan(0, 1, 0),
        };
        _clockTimer.Tick += ClockTimer_Tick;

        _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250),
        };
        _countdownTimer.Tick += CountdownTimer_Tick;

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
        _clockTimer.IsEnabled = true;
        _clockTimer.Start();
        ClockText = DateTime.Now.ToString("HH:mm");
    }

    private void ClockTimer_Tick(object? sender, EventArgs e)
    {
        ClockText = DateTime.Now.ToString("HH:mm");
    }

    private void CountdownTimer_Tick(object? sender, EventArgs e)
    {
        TimeSpan remaining = _countdownEndsAt - DateTimeOffset.Now;
        if (remaining <= TimeSpan.Zero)
        {
            TimeSpan overtime = DateTimeOffset.Now - _countdownEndsAt;
            if (overtime < TimeSpan.FromSeconds(1))
            {
                CountdownText = "00:00";
                CountdownForeground = WarningBrush;
                return;
            }

            TimeSpan displayedOvertime = TimeSpan.FromSeconds(Math.Floor(overtime.TotalSeconds));
            CountdownText = $"-{FormatCountdown(displayedOvertime)}";
            CountdownForeground = ErrorBrush;
            return;
        }

        TimeSpan displayedRemaining = RoundUpToSecond(remaining);
        CountdownText = FormatCountdown(displayedRemaining);
        CountdownForeground = displayedRemaining < TimeSpan.FromMinutes(1)
            ? WarningBrush
            : Brushes.White;
    }

    public void StartCountdown(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return;
        }

        _countdownEndsAt = DateTimeOffset.Now.Add(duration);
        CountdownText = FormatCountdown(duration);
        CountdownForeground = duration < TimeSpan.FromMinutes(1)
            ? WarningBrush
            : Brushes.White;
        IsCountdownVisible = true;
        _countdownTimer.Start();
    }

    public void StopCountdown()
    {
        _countdownTimer.Stop();
        IsCountdownVisible = false;
    }

    private static string FormatCountdown(TimeSpan duration)
    {
        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}"
            : $"{(int)duration.TotalMinutes:00}:{duration.Seconds:00}";
    }

    private static TimeSpan RoundUpToSecond(TimeSpan duration) =>
        TimeSpan.FromSeconds(Math.Ceiling(duration.TotalSeconds));

    public Action? MediaEnded;

    private CancellationTokenSource? _cancellationTokenSource = null;

    public IImage? ImageSource
    {
        get;
        set
        {
            _cancellationTokenSource?.Cancel();
            Opacity = 1;
            SetProperty(ref field, value);
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

    [ObservableProperty]
    private bool _isCountdownVisible;

    [ObservableProperty]
    private string _countdownText = "00:00";

    [ObservableProperty]
    private IBrush _countdownForeground = Brushes.White;

    public string? BannerText
    {
        get => string.IsNullOrWhiteSpace(field) ? null : string.Concat(Enumerable.Range(0, 20).Select(x => $"{field.Trim()} +++ ")).Trim();
        set => SetProperty(ref field, value);
    } = null;

    public string? ClockText
    {
        get;
        set => SetProperty(ref field, value);
    }

    public SettingsViewModel Settings { get; set; }
    public double TextSize => GlobalConfig.JsonFile.Settings.BannerSettings.TextSize;

    public void FirePropertyChanged()
    {
        // TODO Make the property reactive
        OnPropertyChanged(nameof(TextSize));
    }
}

using Avalonia.Media;
using Avalonia.Threading;
using ChurchProjector.Classes;
using ChurchProjector.Views.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using System;
using System.IO;
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
    private readonly LibVLC? _libVlc;

    public ImageViewModel(SettingsViewModel settings)
    {
        Settings = settings;

        try
        {
            _libVlc = new LibVLC();
            MediaPlayer = new MediaPlayer(_libVlc);
            PreviewMediaPlayer = new MediaPlayer(_libVlc)
            {
                Mute = true,
                Volume = 0,
            };
            PreviewMediaPlayer.Playing += (_, _) =>
            {
                PreviewMediaPlayer.Mute = true;
                PreviewMediaPlayer.Volume = 0;
                PreviewMediaPlayer.SetAudioTrack(-1);
            };
            PreviewMediaPlayer.TimeChanged += (_, _) =>
            {
                if (_pausePreviewWhenReady)
                {
                    _pausePreviewWhenReady = false;
                    Dispatcher.UIThread.Post(() => PreviewMediaPlayer.SetPause(true));
                }
            };
            PreviewMediaPlayer.LengthChanged += (_, e) => Dispatcher.UIThread.Post(() =>
            {
                if (!IsMediaPresented)
                {
                    MediaDuration = Math.Max(0, e.Length);
                }
            });
            PreviewMediaPlayer.SeekableChanged += (_, e) => Dispatcher.UIThread.Post(() =>
            {
                if (!IsMediaPresented)
                {
                    IsMediaSeekable = e.Seekable != 0;
                }
            });
            MediaPlayer.EndReached += (_, _) => Dispatcher.UIThread.Post(() =>
            {
                IsMediaPaused = true;
                IsMediaEnded = true;
                IsMediaPresented = false;
                MediaTime = MediaDuration;
                MediaSeekPosition = MediaDuration;
                MediaEnded?.Invoke();
            });
            MediaPlayer.TimeChanged += (_, e) => Dispatcher.UIThread.Post(() =>
            {
                long reportedTime = Math.Max(0, e.Time);
                if (_pendingSeekPosition is double pendingSeekPosition)
                {
                    if (IsMediaPaused || Math.Abs(MediaPlayer.Position - pendingSeekPosition) > 0.01)
                    {
                        return;
                    }

                    _pendingSeekPosition = null;
                }

                MediaTime = reportedTime;
                if (!_isUserSeeking)
                {
                    MediaSeekPosition = MediaTime;
                }
            });
            MediaPlayer.LengthChanged += (_, e) => Dispatcher.UIThread.Post(() => MediaDuration = Math.Max(0, e.Length));
            MediaPlayer.SeekableChanged += (_, e) => Dispatcher.UIThread.Post(() => IsMediaSeekable = e.Seekable != 0);
            MediaPlayer.Playing += (_, _) => Dispatcher.UIThread.Post(() =>
            {
                IsMediaPaused = false;
                IsMediaEnded = false;
                if (_pendingSeekPosition is not null)
                {
                    _ = ApplyPendingSeekAsync(_seekRequestVersion);
                }
            });
            MediaPlayer.Paused += (_, _) => Dispatcher.UIThread.Post(() => IsMediaPaused = true);
        }
        catch
        {
            MediaPlayer?.Dispose();
            PreviewMediaPlayer?.Dispose();
            _libVlc?.Dispose();
        }

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

    public MediaPlayer? MediaPlayer { get; }

    public MediaPlayer? PreviewMediaPlayer { get; }

    public bool IsMediaAvailable => MediaPlayer is not null && PreviewMediaPlayer is not null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentMediaName))]
    [NotifyPropertyChangedFor(nameof(ShowMediaControls))]
    private string? _currentMediaPath;

    public string? CurrentMediaName => Path.GetFileName(CurrentMediaPath);

    public bool CurrentMediaIsAudio => CurrentFileType == FileType.Audio;

    [ObservableProperty]
    private bool _isMediaPaused;

    [ObservableProperty]
    private bool _isMediaEnded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowOutputMedia))]
    [NotifyPropertyChangedFor(nameof(ShowImage))]
    private bool _isMediaPresented;

    public bool ShowOutputMedia => ShowVideo && IsMediaPresented;

    [ObservableProperty]
    private bool _isMediaSeekable;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MediaTimeText))]
    private long _mediaTime;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MediaDurationText))]
    [NotifyPropertyChangedFor(nameof(MediaProgressMaximum))]
    private long _mediaDuration;

    [ObservableProperty]
    private double _mediaSeekPosition;

    private bool _isUserSeeking;
    private bool _pausePreviewWhenReady;
    private double? _pendingSeekPosition;
    private int _seekRequestVersion;

    public long MediaProgressMaximum => Math.Max(1, MediaDuration);

    public string MediaTimeText => FormatMediaTime(MediaTime);

    public string MediaDurationText => FormatMediaTime(MediaDuration);

    private static string FormatMediaTime(long milliseconds)
    {
        TimeSpan time = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
        return time.TotalHours >= 1
            ? $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}"
            : $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
    }

    [ObservableProperty]
    private bool _isMediaMuted;

    [ObservableProperty]
    private double _mediaVolume = 100;

    partial void OnIsMediaMutedChanged(bool value)
    {
        if (!value && MediaVolume <= 0)
        {
            MediaVolume = 50;
        }

        ApplyOutputVolume();
    }

    partial void OnMediaVolumeChanged(double value)
    {
        if (value <= 0)
        {
            IsMediaMuted = true;
        }
        else if (IsMediaMuted)
        {
            IsMediaMuted = false;
        }

        ApplyOutputVolume();
    }

    private void ApplyOutputVolume()
    {
        if (MediaPlayer is null)
        {
            return;
        }

        MediaPlayer.Mute = IsMediaMuted;
        MediaPlayer.Volume = IsMediaMuted ? 0 : (int)Math.Round(MediaVolume);
    }

    public bool PlayMedia(string filePath)
    {
        if (!IsMediaAvailable || _libVlc is null || !File.Exists(filePath))
        {
            return false;
        }

        StopMedia();
        CurrentFileType = FileExtensions.GetFileType(Path.GetExtension(filePath)) ?? FileType.Movie;
        CurrentMediaPath = filePath;
        MediaTime = 0;
        MediaSeekPosition = 0;
        MediaDuration = 0;
        IsMediaEnded = false;
        IsMediaSeekable = false;
        IsMediaPresented = false;
        _pendingSeekPosition = null;
        _seekRequestVersion++;

        using Media outputMedia = new(_libVlc, filePath);
        MediaPlayer!.Media = outputMedia;
        using Media previewMedia = new(_libVlc, filePath);
        _pausePreviewWhenReady = true;
        PreviewMediaPlayer!.Play(previewMedia);

        IsMediaPaused = true;
        ApplyOutputVolume();
        return true;
    }

    [RelayCommand]
    private void RestartMedia()
    {
        if (!IsMediaAvailable)
        {
            return;
        }

        if (IsMediaEnded || MediaPlayer!.State == VLCState.Ended)
        {
            RestartEndedMedia();
            return;
        }

        RequestSeek(0);
    }

    [RelayCommand]
    private void SeekMedia(int seconds)
    {
        if (!IsMediaAvailable || !IsMediaSeekable || MediaDuration <= 0)
        {
            return;
        }

        RequestSeek(MediaTime + seconds * 1000L);
    }

    [RelayCommand]
    private void ToggleMediaPlayback()
    {
        if (!IsMediaAvailable)
        {
            return;
        }

        if (IsMediaEnded || MediaPlayer!.State == VLCState.Ended)
        {
            RestartEndedMedia();
            return;
        }

        if (MediaPlayer.State == VLCState.Playing)
        {
            MediaPlayer.SetPause(true);
            PreviewMediaPlayer!.SetPause(true);

            IsMediaPaused = true;
            return;
        }

        if (MediaPlayer.State == VLCState.Paused)
        {
            MediaPlayer.SetPause(false);
            PreviewMediaPlayer!.SetPause(false);

            IsMediaPaused = false;
            if (_pendingSeekPosition is not null)
            {
                _ = ApplyPendingSeekAsync(_seekRequestVersion);
            }
            return;
        }

        StartLoadedMedia();
    }

    private void RestartEndedMedia()
    {
        MediaPlayer!.Stop();
        PreviewMediaPlayer!.Stop();

        MediaTime = 0;
        MediaSeekPosition = 0;
        IsMediaEnded = false;
        _pendingSeekPosition = 0;
        _seekRequestVersion++;
        StartLoadedMedia();
    }

    private void StartLoadedMedia()
    {
        _pausePreviewWhenReady = false;
        bool outputStarted = MediaPlayer?.Play() ?? false;
        bool previewStarted;
        if (PreviewMediaPlayer?.State == VLCState.Paused)
        {
            PreviewMediaPlayer.SetPause(false);
            previewStarted = true;
        }
        else
        {
            previewStarted = PreviewMediaPlayer?.Play() ?? false;
        }
        IsMediaPresented = outputStarted;
        IsMediaPaused = !(outputStarted && previewStarted);

        if (PreviewMediaPlayer is not null)
        {
            PreviewMediaPlayer.Mute = true;
            PreviewMediaPlayer.Volume = 0;
            PreviewMediaPlayer.SetAudioTrack(-1);
        }

        ApplyOutputVolume();
    }

    public void BeginMediaSeek()
    {
        _isUserSeeking = true;
    }

    public void CompleteMediaSeek(double targetTime)
    {
        _isUserSeeking = false;
        RequestSeek((long)targetTime);
    }

    private void RequestSeek(long targetTime)
    {
        if (!IsMediaSeekable || MediaDuration <= 0)
        {
            return;
        }

        long safeTime = Math.Clamp(targetTime, 0, Math.Max(0, MediaDuration - 250));
        _pendingSeekPosition = Math.Clamp((double)safeTime / MediaDuration, 0, 0.999);
        _seekRequestVersion++;
        MediaTime = safeTime;
        MediaSeekPosition = safeTime;
        ApplySeekPosition(_pendingSeekPosition.Value);

        if (!IsMediaPaused && IsMediaPresented)
        {
            _ = ApplyPendingSeekAsync(_seekRequestVersion);
        }
    }

    private void ApplySeekPosition(double position)
    {
        try
        {
            if (PreviewMediaPlayer is { IsSeekable: true })
            {
                PreviewMediaPlayer.Position = (float)position;
            }

            if (IsMediaPresented && MediaPlayer is { IsSeekable: true })
            {
                MediaPlayer.Position = (float)position;
            }
        }
        catch
        {
            // LibVLC can reject a seek while the input is changing state. Ignore that transient request.
        }
    }

    private async Task ApplyPendingSeekAsync(int requestVersion)
    {
        for (int attempt = 0; attempt < 4; attempt++)
        {
            await Task.Delay(75);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (requestVersion == _seekRequestVersion && _pendingSeekPosition is double position)
                {
                    ApplySeekPosition(position);
                }
            });
        }
    }

    public void StopMedia()
    {
        MediaPlayer?.Stop();
        PreviewMediaPlayer?.Stop();
        IsMediaPaused = true;
        IsMediaEnded = false;
        IsMediaSeekable = false;
        IsMediaPresented = false;
        _pausePreviewWhenReady = false;
        _pendingSeekPosition = null;
        _seekRequestVersion++;
        MediaTime = 0;
        MediaSeekPosition = 0;
        MediaDuration = 0;
    }

    private CancellationTokenSource? _cancellationTokenSource = null;

    public IImage? ImageSource
    {
        get;
        set
        {
            _cancellationTokenSource?.Cancel();
            Opacity = 1;
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(ShowImage));
            }
        }
    }

    [ObservableProperty]
    private double _opacity = 1;

    public void HideImage(bool fadeOut)
    {
        _cancellationTokenSource?.Cancel();
        if (CurrentMediaPath is not null)
        {
            StopMedia();
            ImageSource = null;
            return;
        }

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
    [NotifyPropertyChangedFor(nameof(CurrentMediaIsAudio))]
    [NotifyPropertyChangedFor(nameof(ShowMediaControls))]
    [NotifyPropertyChangedFor(nameof(ShowImage))]
    [NotifyPropertyChangedFor(nameof(ShowOutputMedia))]
    private FileType? _currentFileType;
    public bool ShowVideo => CurrentFileType == FileType.Movie;
    public bool ShowMediaControls => CurrentMediaPath is not null && CurrentFileType is FileType.Movie or FileType.Audio;
    public bool ShowImage => ImageSource is not null && !IsMediaPresented;

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

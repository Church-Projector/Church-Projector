using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using ChurchProjector.Classes;
using ChurchProjector.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChurchProjector.Views.Settings;

public partial class SettingsViewModel : ObservableObject
{
    public void SetMonitors(IDictionary<string, Screen> monitors)
    {
        Monitors = monitors;
    }

    public Classes.Settings Settings => GlobalConfig.JsonFile.Settings;

    #region Events
    public event Action? RecreateBanner;
    #endregion

    #region Monitors
    public IDictionary<string, Screen> Monitors { get; internal set; } = new Dictionary<string, Screen>();
    // TODO: https://github.com/AvaloniaUI/Avalonia/issues/11512
    public KeyValuePair<string, Screen> SelectedMonitore
    {
        get => GlobalConfig.JsonFile.Settings.SelectedMonitorName != null && Monitors.ContainsKey(GlobalConfig.JsonFile.Settings.SelectedMonitorName) ? Monitors.Single(x => x.Key == GlobalConfig.JsonFile.Settings.SelectedMonitorName) : GetFirstMonitor();
        set
        {
            if (GlobalConfig.JsonFile.Settings.SelectedMonitorName != value.Key)
            {
                if (value.Key == null)
                {
                    value = GetFirstMonitor();
                }

                GlobalConfig.JsonFile.Settings.SelectedMonitorName = value.Key;
                OnPropertyChanged();

                WindowState = WindowState.Normal;
                OnPropertyChanged(nameof(Position));
                WindowState = WindowState.FullScreen;
            }
        }
    }

    private KeyValuePair<string, Screen> GetFirstMonitor()
    {
        KeyValuePair<string, Screen> screen = Monitors.FirstOrDefault(x => !x.Value.IsPrimary);
        if (screen.Key == null)
        {
            screen = Monitors.FirstOrDefault();
        }
        return screen;
    }

    public PixelPoint Position
    {
        get => SelectedMonitore.Value.Bounds.Position;
    }

    public string BannerFontSizeText => $"{Lang.Resources.FontSize_Colon} {BannerFontSize}";
    #endregion

    public List<Theme> Themes { get; } = Enum.GetValues<Theme>().ToList();
    public Theme Theme
    {
        get => GlobalConfig.JsonFile.Settings.Theme;
        set
        {
            if (GlobalConfig.JsonFile.Settings.Theme != value)
            {
                GlobalConfig.JsonFile.Settings.Theme = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ThemeName));
            }
        }
    }
    public string ThemeName => Theme == Theme.Light ? "Light" : "Dark";

    public List<Language> Languages { get; } = Enum.GetValues<Language>().ToList();
    public Language Language
    {
        get => GlobalConfig.JsonFile.Settings.Language;
        set
        {
            if (GlobalConfig.JsonFile.Settings.Language != value)
            {
                GlobalConfig.JsonFile.Settings.Language = value;
                OnPropertyChanged();
            }
        }
    }

    #region display
    [ObservableProperty]
    private WindowState _windowState = WindowState.FullScreen;
    #endregion

    public bool ShowClock
    {
        get => GlobalConfig.JsonFile.Settings.ShowClock;
        set => GlobalConfig.JsonFile.Settings.ShowClock = value;
    }

    #region paths
    public string? BiblesPath
    {
        get => GlobalConfig.JsonFile.Settings.PathSettings.BiblePath;
        set
        {
            GlobalConfig.JsonFile.Settings.PathSettings.BiblePath = value;
            OnPropertyChanged();
        }
    }

    public string? SongsPath
    {
        get => GlobalConfig.JsonFile.Settings.PathSettings.SongPath;
        set
        {
            GlobalConfig.JsonFile.Settings.PathSettings.SongPath = value;
            OnPropertyChanged();
        }
    }
    #endregion

    #region banner
    public int BannerSpeed
    {
        get => GlobalConfig.JsonFile.Settings.BannerSettings.Speed;
        set
        {
            GlobalConfig.JsonFile.Settings.BannerSettings.Speed = value;
            RecreateBanner?.Invoke();
            OnPropertyChanged();
            DecreaseBannerSpeedCommand.NotifyCanExecuteChanged();
            IncreaseBannerSpeedCommand.NotifyCanExecuteChanged();
        }
    }

    public int BannerFontSize
    {
        get => GlobalConfig.JsonFile.Settings.BannerSettings.TextSize;
        set
        {
            GlobalConfig.JsonFile.Settings.BannerSettings.TextSize = Math.Min(50, Math.Max(10, value));
            RecreateBanner?.Invoke();
            OnPropertyChanged();
            OnPropertyChanged(nameof(BannerFontSizeText));
            DecreaseBannerSizeCommand.NotifyCanExecuteChanged();
            IncreaseBannerSizeCommand.NotifyCanExecuteChanged();
        }
    }

    public int BannerFrames
    {
        get => GlobalConfig.JsonFile.Settings.BannerSettings.Fps;
        set
        {
            GlobalConfig.JsonFile.Settings.BannerSettings.Fps = value;
            OnPropertyChanged();
            RecreateBanner?.Invoke();
        }
    }
    #endregion

    #region bible settings
    public List<BookLanguage> BookLanguages { get; } = Enum.GetValues<BookLanguage>().ToList();
    public BookLanguage BookLanguage
    {
        get => GlobalConfig.JsonFile.Settings.BibleSettings.BookLanguage;
        set => this.SetProperty(GlobalConfig.JsonFile.Settings.BibleSettings.BookLanguage, value, (newValue) => GlobalConfig.JsonFile.Settings.BibleSettings.BookLanguage = newValue);
    }
    #endregion

    #region Commands
    private bool MayIncreaseBannerSpeed => BannerSpeed < 100;
    [RelayCommand(CanExecute = nameof(MayIncreaseBannerSpeed))]
    private void IncreaseBannerSpeed()
    {
        BannerSpeed++;
    }

    private bool MayDecreaseBannerSpeed => BannerSpeed > 1;
    [RelayCommand(CanExecute = nameof(MayDecreaseBannerSpeed))]
    private void DecreaseBannerSpeed()
    {
        BannerSpeed--;
    }

    private bool MayIncreaseBannerSize => BannerFontSize < 50;
    [RelayCommand(CanExecute = nameof(MayIncreaseBannerSize))]
    private void IncreaseBannerSize()
    {
        BannerFontSize++;
    }

    private bool MayDecreaseBannerSize => BannerFontSize > 10;
    [RelayCommand(CanExecute = nameof(MayDecreaseBannerSize))]
    private void DecreaseBannerSize()
    {
        BannerFontSize--;
    }
    #endregion
}

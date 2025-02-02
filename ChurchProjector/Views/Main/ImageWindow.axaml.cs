using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ChurchProjector.Classes;
using ChurchProjector.Views.Settings;
using System;
using System.Linq;
using System.Threading;

namespace ChurchProjector.Views.Main;

/// <summary>
/// Interaction logic for EasyImageWindow.xaml
/// </summary>
public partial class ImageWindow : Window
{
    private const double BannerPadding = 10.0; // Zusätzlicher Padding für den Banner-Text
    private const int REPETITION_COUNT = 2;

    public ImageViewModel ViewModel { get; }

    public void CreateBanner()
    {
        if (string.IsNullOrWhiteSpace(ViewModel.BannerText))
        {
            return;
        }
        ViewModel.IsBannerVisible = true;
        SpBanner.Children.Clear();
        for (int i = 0; i < REPETITION_COUNT; i++)
        {
            TextBlock bannerText = new()
            {
                Text = ViewModel.BannerText,
                FontSize = ViewModel.TextSize,
                Foreground = Brushes.White,
                Margin = new Thickness(BannerPadding),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            SpBanner.Children.Add(bannerText);
        }

        StartBannerAnimation();
    }

    CancellationTokenSource cancellationTokenSource = new();

    private void StartBannerAnimation()
    {
        cancellationTokenSource.Cancel();
        cancellationTokenSource = new();
        CancellationToken token = cancellationTokenSource.Token;
        DispatcherTimer.Run(() =>
        {
            if (token.IsCancellationRequested)
            {
                return false;
            }
            TextBlock textBlock = SpBanner.Children.Cast<TextBlock>().FirstOrDefault()!;
            if (textBlock == null)
            {
                return false;
            }
            double bannerSpeed = GlobalConfig.JsonFile.Settings.BannerSettings.Speed / 10d;
            textBlock.Margin = new Thickness(textBlock.Margin.Left - bannerSpeed, BannerPadding, BannerPadding, BannerPadding);

            if (-textBlock.Margin.Left > textBlock.Bounds.Width + BannerPadding)
            {
                SpBanner.Children.Remove(textBlock);
                textBlock.Margin = new Thickness(BannerPadding);
                SpBanner.Children.Add(textBlock);
            }

            return SpBanner.IsVisible;
        }, TimeSpan.FromSeconds(1d / GlobalConfig.JsonFile.Settings.BannerSettings.Fps));
    }

    public ImageWindow()
    {
        ViewModel = null!;
        InitializeComponent();
    }

    public ImageWindow(SettingsViewModel settings)
    {
        InitializeComponent();
        DataContext = ViewModel = new ImageViewModel(settings);

        // Subcribe to position changes, this is because we can't bind the position.
        Position = ViewModel.Settings.Position;

        ViewModel.Settings.PropertyChanged += Settings_PropertyChanged;
        ViewModel.Settings.PropertyChanged += SettingsViewModel_PropertyChanged;
    }

    private void Settings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Position))
        {
            Position = ViewModel.Settings.Position;
        }
    }

    private void SettingsViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.Settings.WindowState))
        {
            if (ViewModel.Settings.WindowState == WindowState.FullScreen)
            {
                var screen = ViewModel.Settings.SelectedMonitore.Value;
                Width = screen.Bounds.Width;
                Height = screen.Bounds.Height;
            }
        }
    }

    internal void StopBanner()
    {
        ViewModel.IsBannerVisible = false;
        SpBanner.Children.Clear();
    }
}

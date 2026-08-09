using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace ChurchProjector.Views.Timer;

public partial class TimerWindow : Window
{
    public TimerWindow()
    {
        InitializeComponent();
    }

    public required Action<TimeSpan> StartTimer { get; init; }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void StartButton_OnClick(object? sender, RoutedEventArgs e)
    {
        int minutes = Convert.ToInt32(MinutesInput.Value ?? 0);
        int seconds = Convert.ToInt32(SecondsInput.Value ?? 0);
        TimeSpan duration = TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);

        if (duration <= TimeSpan.Zero)
        {
            ValidationText.IsVisible = true;
            return;
        }

        StartTimer(duration);
        Close();
    }
}

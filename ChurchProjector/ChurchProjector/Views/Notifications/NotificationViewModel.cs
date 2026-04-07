using ChurchProjector.Views.Notification;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChurchProjector.Views.Notifications;

public partial class NotificationViewModel(string message, NotificationType type, bool isRunning)
    : ObservableObject
{
    public string Message { get; } = message;
    public NotificationType Type { get; } = type;
    public bool IsRunning { get; set; } = isRunning;
    
    [ObservableProperty]
    private double progress = 1.0;

    [ObservableProperty]
    private bool isPaused;

}
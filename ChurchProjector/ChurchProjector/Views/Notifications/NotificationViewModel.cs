using ChurchProjector.Views.Notification;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChurchProjector.Views.Notifications;

public partial class NotificationViewModel(string message, NotificationType type = NotificationType.Error)
    : ObservableObject
{
    public string Message { get; } = message;
    public NotificationType Type { get; } = type;
}
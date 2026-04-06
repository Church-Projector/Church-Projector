using ChurchProjector.Views.Notification;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChurchProjector.Views.Notifications;

public partial class NotificationViewModel : ObservableObject
{
    public string Message { get; }
    public NotificationType Type { get; }

    public NotificationViewModel(string message, NotificationType type = NotificationType.Error)
    {
        Message = message;
        Type = type;
    }
}
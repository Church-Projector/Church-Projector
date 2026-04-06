using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using ChurchProjector.Views.Notification;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChurchProjector.Views.Notifications;

public partial class NotificationService : ObservableObject
{
    public ObservableCollection<NotificationViewModel> Notifications { get; } = [];

    public void Show(string message, NotificationType type = NotificationType.Info, int durationMs = 3000)
    {
        var notification = new NotificationViewModel(message, type);
        Notifications.Add(notification);

        if (durationMs > 0)
        {
            _ = Task.Delay(durationMs).ContinueWith(_ =>
                Dispatcher.UIThread.Post(() => Notifications.Remove(notification)));
        }
    }
}
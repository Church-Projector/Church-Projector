using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using ChurchProjector.Views.Notification;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChurchProjector.Views.Notifications;

public partial class NotificationService : ObservableObject
{
    public ObservableCollection<NotificationViewModel> Notifications { get; } = [];
    
    public void Show(string message, NotificationType type, int durationMs = 5000)
    {
        var notification = new NotificationViewModel(message, type, durationMs > 0);
        Notifications.Add(notification);

        if (durationMs <= 0)
            return;

        _ = RunTimer(notification, durationMs);
    }

    private async Task RunTimer(NotificationViewModel notification, int durationMs)
    {
        const int interval = 50; // ms
        var elapsed = 0;

        while (elapsed < durationMs)
        {
            await Task.Delay(interval);

            if (notification.IsPaused)
                continue;

            elapsed += interval;

            var progress = 1.0 - (double)elapsed / durationMs;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                notification.Progress = progress;
            });
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Notifications.Remove(notification);
        });
    }
}
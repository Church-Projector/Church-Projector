using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace ChurchProjector.Views.Notification;
public class NotificationViewModel : ObservableObject
{
    public string Notification
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasNotification));
            }
        }
    } = string.Empty;

    public bool HasNotification => !string.IsNullOrWhiteSpace(Notification);

    public required ICommand ShowNotificationCommand { get; init; }
    public required ICommand CloseDialogCommand { get; init; }
}

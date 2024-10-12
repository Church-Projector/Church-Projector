using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace ChurchProjector.Views.Notification;
public class NotificationViewModel : ObservableObject
{
    private string _notification = string.Empty;
    public string Notification
    {
        get => _notification;
        set
        {
            if (_notification != value)
            {
                _notification = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasNotification));
            }
        }
    }
    public bool HasNotification => !string.IsNullOrWhiteSpace(Notification);

    public required ICommand ShowNotificationCommand { get; init; }
    public required ICommand CloseDialogCommand { get; init; }
}

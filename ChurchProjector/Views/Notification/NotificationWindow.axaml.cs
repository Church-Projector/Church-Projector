using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using System;

namespace ChurchProjector.Views.Notification;
public partial class NotificationWindow : Window
{
    private readonly NotificationViewModel _viewModel;

    public NotificationWindow()
    {
        InitializeComponent();

        _viewModel = new NotificationViewModel()
        {
            CloseDialogCommand = new RelayCommand(Close),
            ShowNotificationCommand = new RelayCommand(() =>
            {
                if (_viewModel is null)
                {
                    return;
                }

                ShowNotification?.Invoke(_viewModel.Notification.Trim());
                Close();
            }),
        };
        DataContext = _viewModel;
        TxtNotification.AttachedToVisualTree += TxtNotification_AttachedToVisualTree;
    }

    public required Action<string> ShowNotification { get; set; }

    // https://github.com/AvaloniaUI/Avalonia/issues/4835#issuecomment-706530463
    private void TxtNotification_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        TxtNotification.Focus();
    }
}

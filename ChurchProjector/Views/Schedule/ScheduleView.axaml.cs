using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using ChurchProjector.Classes.Schedule;
using System.Linq;

namespace ChurchProjector.Views.Schedule;
public partial class ScheduleView : UserControl
{
    public ScheduleView()
    {
        InitializeComponent();

        DragDrop.SetAllowDrop(LboSchedules, true);
        AddHandler(DragDrop.DropEvent, Drop);
        AddHandler(DragDrop.DragOverEvent, DragOver);
    }


    private void Drop(object? sender, DragEventArgs e)
    {
        if (DataContext is not ScheduleViewModel viewModel)
        {
            return;
        }
        if (e.Data.GetFiles() is { } fileNames)
        {
            foreach (Avalonia.Platform.Storage.IStorageItem file in fileNames)
            {
                viewModel.Schedules.Add(ScheduleEntry.FromFile(file.Path.LocalPath));
            }
        }
        else if (e.Data.Get(nameof(ScheduleEntry)) is ScheduleEntry scheduleEntry)
        {
            Point position = e.GetPosition(LboSchedules);
            ScheduleEntry? otherEntry = LboSchedules.GetVisualDescendants()
                .OfType<ListBoxItem>()
                .OrderBy(lbi => lbi.Bounds.Y)
                .LastOrDefault(x => x.Bounds.Y <= position.Y)
                ?.DataContext as ScheduleEntry;
            otherEntry ??= viewModel.Schedules.First();
            int otherIndex = viewModel.Schedules.IndexOf(otherEntry);
            viewModel.Schedules.Remove(scheduleEntry);
            viewModel.Schedules.Insert(otherIndex, scheduleEntry);
        }
    }

    private async void Svg_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not ScheduleViewModel viewModel || sender is not Visual visual || visual.DataContext is not ScheduleEntry scheduleEntry)
        {
            return;
        }

        Point mousePos = e.GetPosition(LboSchedules);
        DataObject dragData = new();
        dragData.Set(nameof(ScheduleEntry), scheduleEntry);
        await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Move);
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        if (DataContext is not ScheduleViewModel viewModel)
        {
            return;
        }
        if (e.Data.Get(nameof(ScheduleEntry)) is ScheduleEntry scheduleEntry)
        {
            Point position = e.GetPosition(LboSchedules);
            ScheduleEntry? otherEntry = LboSchedules.GetVisualDescendants()
                .OfType<ListBoxItem>()
                .OrderBy(lbi => lbi.Bounds.Y)
                .LastOrDefault(x => x.Bounds.Y <= position.Y)
                ?.DataContext as ScheduleEntry;
            otherEntry ??= viewModel.Schedules.First();
            if (otherEntry == scheduleEntry)
            {
                return;
            }
            int otherIndex = viewModel.Schedules.IndexOf(otherEntry);
            viewModel.Schedules.Remove(scheduleEntry);
            viewModel.Schedules.Insert(otherIndex, scheduleEntry);
        }
    }
}

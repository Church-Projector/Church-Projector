using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using ChurchProjector.Classes.Schedule;
using System.Linq;
using Avalonia.Platform.Storage;

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

        var files = e.DataTransfer.TryGetFiles();
        if (files is not null)
        {
            foreach (IStorageItem file in files)
            {
                viewModel.Schedules.Add(ScheduleEntry.FromFile(file.Path.LocalPath));
            }
        }
        else if (e.DataTransfer is ScheduleEntryDataTransfer { ScheduleEntry: { } scheduleEntry })
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
        if (DataContext is not ScheduleViewModel viewModel || sender is not Visual visual ||
            visual.DataContext is not ScheduleEntry scheduleEntry)
        {
            return;
        }

        await DragDrop.DoDragDropAsync(e, new ScheduleEntryDataTransfer() { ScheduleEntry = scheduleEntry },
            DragDropEffects.Move);
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        if (DataContext is not ScheduleViewModel viewModel)
        {
            return;
        }

        if (e.DataTransfer is ScheduleEntryDataTransfer { ScheduleEntry: { } scheduleEntry })
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
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using ChurchProjector.Classes;
using ChurchProjector.Classes.Schedule;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChurchProjector.Views.Schedule;
public partial class ScheduleViewModel : ObservableObject
{
    private readonly IStorageProvider _storageProvider;
    private readonly IClipboard _clipboard;
    private readonly bool _powerPointAvailable;
    private readonly bool _libVlcAvailable;
    public ScheduleViewModel(IStorageProvider storageProvider, IClipboard clipboard, bool powerPointAvailable, bool libVlcAvailable)
    {
        _storageProvider = storageProvider;
        _clipboard = clipboard;
        Schedules = new ObservableCollection<ScheduleEntry>(GlobalConfig.JsonFile.Schedules.ConvertAll(ScheduleEntry.FromFile));
        Schedules.CollectionChanged += Schedules_CollectionChanged;
        _powerPointAvailable = powerPointAvailable;
        _libVlcAvailable = libVlcAvailable;
        UpdateErrors();
    }

    private void Schedules_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        GlobalConfig.JsonFile.Schedules = Schedules.Select(x => x.FilePath).ToList();
    }

    public ObservableCollection<ScheduleEntry> Schedules { get; }
    private ICommand? _openScheduleEntryCommand;
    public ICommand? OpenScheduleEntryCommand { get => _openScheduleEntryCommand; set => SetProperty(ref _openScheduleEntryCommand, value); }

    [RelayCommand]
    private void RemoveScheduleEntry(ScheduleEntry scheduleEntry)
    {
        Schedules.Remove(scheduleEntry);
    }

    [RelayCommand]
    private async Task AddScheduleEntry()
    {
        IReadOnlyList<IStorageFile> files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            AllowMultiple = true,
        });
        foreach (IStorageFile file in files)
        {
            Schedules.Add(ScheduleEntry.FromFile(file.Path.LocalPath));
        }
        UpdateErrors();
    }

    [RelayCommand]
    private async Task CopyFilePath(ScheduleEntry scheduleEntry)
    {
        try
        {
            await _clipboard.SetTextAsync(scheduleEntry.FilePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error copying file path.");
        }
    }

    [RelayCommand]
    private void OpenFilePath(ScheduleEntry scheduleEntry)
    {
        try
        {
            if (!File.Exists(scheduleEntry.FilePath))
            {
                return;
            }

            string argument = $"/select, \"{scheduleEntry.FilePath}\"";

            System.Diagnostics.Process.Start("explorer.exe", argument);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error opening a file path.");
        }
    }

    [RelayCommand]
    private void DeleteFilePath(ScheduleEntry scheduleEntry)
    {
        try
        {
            File.Delete(scheduleEntry.FilePath);
            Schedules.Remove(scheduleEntry);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting a file.");
        }
    }

    private void UpdateErrors()
    {
        foreach (ScheduleEntry scheduleEntry in Schedules)
        {
            if (FileExtensions.GetFileType(scheduleEntry.FileType) == FileType.Movie)
            {
                scheduleEntry.ApplicationExists = _libVlcAvailable;
            }
            else if (FileExtensions.GetFileType(scheduleEntry.FileType) == FileType.Powerpoint)
            {
                scheduleEntry.ApplicationExists = _powerPointAvailable;
            }
            else
            {
                scheduleEntry.ApplicationExists = true;
            }
        }
    }
}

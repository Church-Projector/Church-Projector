using System;
using System.Collections.Generic;
using Avalonia.Input;

namespace ChurchProjector.Classes.Schedule;

public class ScheduleEntryDataTransfer : IDataTransfer
{
    public ScheduleEntry? ScheduleEntry { get; set; }

    IReadOnlyList<DataFormat> IDataTransfer.Formats => [];
    IReadOnlyList<IDataTransferItem> IDataTransfer.Items => [];
    void IDisposable.Dispose() { }
}
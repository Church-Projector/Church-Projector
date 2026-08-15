using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ChurchProjector.Classes;
using System;
using System.Globalization;

namespace ChurchProjector.Converters;

public class FileTypeIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string? resourceKey = value switch
        {
            FileType.Audio => "AudioFileIcon",
            FileType.Image => "ImageFileIcon",
            FileType.Movie => "MovieFileIcon",
            FileType.Pdf => "PdfFileIcon",
            FileType.Powerpoint => "PowerPointFileIcon",
            FileType.Song => "LyricsFileIcon",
            _ => null
        };

        if (resourceKey is null || Application.Current is not { } application)
        {
            return null;
        }

        return application.TryFindResource(resourceKey, out object? resource)
            ? resource as StreamGeometry
            : null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

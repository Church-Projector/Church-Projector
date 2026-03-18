using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ChurchProjector.Converters;

public class StringToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Color.Parse((string)value);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(value);
        Color color = (Color)value;
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
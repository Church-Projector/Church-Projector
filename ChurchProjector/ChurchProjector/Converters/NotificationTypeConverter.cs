using System;
using System.Globalization;
using Avalonia.Data.Converters;
namespace ChurchProjector.Converters;

public class NotificationTypeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        // Parameter kommt als string → in Enum parsen
        if (Enum.TryParse(value.GetType(), parameter.ToString(), out var parsed))
        {
            return value.Equals(parsed);
        }

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
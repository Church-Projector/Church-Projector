using Avalonia.Data.Converters;
using ChurchProjector.Enums;
using System;
using System.Globalization;

namespace ChurchProjector.Converters;

public class EnumDescriptionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Enum @enum)
        {
            return null;
        }
        return @enum.GetDescription();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
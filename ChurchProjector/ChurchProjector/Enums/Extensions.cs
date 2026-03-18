using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ChurchProjector.Enums;
public static class Extensions
{
    public static string? GetDescription<T>(this T value) where T : Enum
    {
        FieldInfo fi = value.GetType().GetField(value.ToString())!;
        DisplayAttribute[] attributes = (DisplayAttribute[])fi.GetCustomAttributes(typeof(DisplayAttribute), false);
        return attributes != null && attributes.Length > 0 ? attributes[0].GetName() : value.ToString();
    }
}

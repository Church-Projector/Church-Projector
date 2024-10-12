using System;
using System.ComponentModel;
using System.Reflection;

namespace ChurchProjector.Enums;
public static class Extensions
{
    public static string GetDescription<T>(this T value) where T : Enum
    {
        FieldInfo fi = value.GetType().GetField(value.ToString())!;
        DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
        return attributes != null && attributes.Length > 0 ? attributes[0].Description : value.ToString();
    }
}

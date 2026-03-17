using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace ChurchProjector.Classes.Configuration.Image;
public class ImageCreationItemConfiguration : ObservableObject
{
    public HorizontalAlignment HorizontalAlignment
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ObservableCollection<Color> Colors { get; set; } =
    [
        // First translation should be white, then red and then random generated.
        new Color("#FFFFFF"),
        new Color("#ECFF80"),
        new Color("#CC83B2"),
    ];
    public string GetColor(int index)
    {
        while (Colors.Count <= index)
        {
            Random r = new();
            string newColor = string.Empty;
            for (int i = 0; i < 3; i++)
            {
                newColor += r.Next(0, 255).ToString("X2");
            }
            Colors.Add(new Color("#" + newColor));
        }
        return Colors[index].Value;
    }

    public float MinFontSize
    {
        get;
        set => SetProperty(ref field, value);
    }

    public float MaxFontSize
    {
        get;
        set => SetProperty(ref field, value);
    }
}
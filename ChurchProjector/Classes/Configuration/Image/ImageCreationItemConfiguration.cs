using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace ChurchProjector.Classes.Configuration.Image;
public class ImageCreationItemConfiguration : ObservableObject
{
    private HorizontalAlignment _horizontalAlignment;
    public HorizontalAlignment HorizontalAlignment { get => _horizontalAlignment; set => SetProperty(ref _horizontalAlignment, value); }
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
    private float _minFontSize;
    public float MinFontSize { get => _minFontSize; set => SetProperty(ref _minFontSize, value); }
    private float _maxFontSize;
    public float MaxFontSize { get => _maxFontSize; set => SetProperty(ref _maxFontSize, value); }
}
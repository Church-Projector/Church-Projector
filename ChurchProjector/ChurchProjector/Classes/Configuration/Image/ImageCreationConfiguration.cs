using CommunityToolkit.Mvvm.ComponentModel;

namespace ChurchProjector.Classes.Configuration.Image;

public class ImageCreationConfiguration : ObservableObject
{
    public ImageCreationItemConfiguration Header
    {
        get;
        set => SetProperty(ref field, value);
    } = new();

    public ImageCreationItemConfiguration Content
    {
        get;
        set => SetProperty(ref field, value);
    } = new();

    public ImageCreationItemConfiguration Bottom
    {
        get;
        set => SetProperty(ref field, value);
    } = new();
}
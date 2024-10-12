using CommunityToolkit.Mvvm.ComponentModel;

namespace ChurchProjector.Classes.Configuration.Image;

public class ImageCreationConfiguration : ObservableObject
{
    private ImageCreationItemConfiguration _header = new();
    public ImageCreationItemConfiguration Header { get => _header; set => SetProperty(ref _header, value); }
    private ImageCreationItemConfiguration _content = new();
    public ImageCreationItemConfiguration Content { get => _content; set => SetProperty(ref _content, value); }
    private ImageCreationItemConfiguration _bottom = new();
    public ImageCreationItemConfiguration Bottom { get => _bottom; set => SetProperty(ref _bottom, value); }
}
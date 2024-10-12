using CommunityToolkit.Mvvm.ComponentModel;

namespace ChurchProjector.Classes.Configuration.Image;
public class Color : ObservableObject
{
    public Color(string value)
    {
        _value = value;
    }
    private string _value;
    public string Value { get => _value; set => SetProperty(ref _value, value); }
}

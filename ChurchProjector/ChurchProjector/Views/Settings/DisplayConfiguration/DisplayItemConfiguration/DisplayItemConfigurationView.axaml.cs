using Avalonia.Controls;
using System;
using System.Linq;
using ChurchProjector.Classes.Configuration.Image;

namespace ChurchProjector.Views.Settings.DisplayConfiguration.DisplayItemConfiguration;
/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class DisplayItemConfigurationView : UserControl
{
    public DisplayItemConfigurationView()
    {
        InitializeComponent();
        CboHorizontalAlignment.ItemsSource = Enum.GetValues<HorizontalAlignment>().ToList();
    }
}

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ChurchProjector.Classes;
using ChurchProjector.Logging;
using ChurchProjector.Views.Init;
using ChurchProjector.Views.Main;
using ChurchProjector.Views.Settings;
using Serilog;
using System.Globalization;

namespace ChurchProjector;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        switch (GlobalConfig.JsonFile.Settings.Language)
        {
            case Enums.Language.Windows:
                Lang.Resources.Culture = CultureInfo.CurrentCulture;
                break;
            case Enums.Language.German:
                Lang.Resources.Culture = new CultureInfo("de-DE");
                break;
            case Enums.Language.English:
                Lang.Resources.Culture = new CultureInfo("en-US");
                break;
        }

        SettingsViewModel settings = new();
        DataContext = settings;

        if (!string.IsNullOrWhiteSpace(GlobalConfig.JsonFile.Settings.PathSettings.SongPath)
            && !string.IsNullOrWhiteSpace(GlobalConfig.JsonFile.Settings.PathSettings.BiblePath))
        {
            InitMainWindow();
        }
        else
        {
            InitWindow initWindow = new();
            initWindow.Closed += InitWindow_Closed;
            initWindow.Show();
        }
    }

    private void InitWindow_Closed(object? sender, System.EventArgs e)
    {
        InitMainWindow();
    }

    private void InitMainWindow()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                .WriteTo.Sink(new ErrorListenerSink())
                .CreateLogger();

            MainWindow mainWindow = new((DataContext as SettingsViewModel)!);
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

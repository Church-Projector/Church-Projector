using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ChurchProjector.Logging;
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
        if (CultureInfo.CurrentCulture.Name == "de-DE")
        {
            Lang.Resources.Culture = new CultureInfo("de-DE");
        }

        SettingsViewModel settings = new();
        DataContext = settings;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                .WriteTo.Sink(new ErrorListenerSink())
                .CreateLogger();

            desktop.MainWindow = new MainWindow(settings);
        }

        base.OnFrameworkInitializationCompleted();
    }
}

using System.Windows;

namespace TravellerTales;

public partial class App : Application
{
    protected async void OnStartup(object sender, StartupEventArgs e)
    {
        var settings = AppSettings.Load();
        AppPaths.Initialize(settings.Data);

        var splashWindow = new SplashWindow();
        MainWindow = splashWindow;
        splashWindow.Show();

        var delaySeconds = Math.Max(0, settings.SplashDurationSeconds);
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

        var mainWindow = new MainWindow(settings);
        MainWindow = mainWindow;
        mainWindow.Show();
        splashWindow.Close();
    }
}

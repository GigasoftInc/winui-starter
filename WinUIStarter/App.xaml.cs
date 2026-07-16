using Microsoft.UI.Xaml;

namespace WinUIStarter;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();

        // Unhandled XAML-thread exceptions are otherwise silent in a Release run started from
        // Explorer -- the process simply vanishes. Logging them somewhere you can read is the
        // single cheapest debugging aid in a real app. Replace with your own logger.
        UnhandledException += (_, e) =>
        {
            System.Diagnostics.Debug.WriteLine("UNHANDLED: " + e.Exception);
            // e.Handled = true;  // set true to keep the app alive
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}

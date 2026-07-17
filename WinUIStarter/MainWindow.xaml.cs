using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace WinUIStarter;

public sealed partial class MainWindow : Window
{
    private int _clicks;

    public MainWindow()
    {
        InitializeComponent();

        // A WinUI Window has no Width/Height. Sizing and centering go through AppWindow.
        // These are physical pixels, so on a 150% display 1000x700 is ~667x467 effective.
        ResizeAndCenter(1000, 700);

        // Window itself never raises Loaded. Hook the root element (or the specific control
        // you need to initialize) instead. Anything that needs a fully realized visual tree --
        // measuring, native interop, a control that renders through a swap chain -- belongs
        // in a Loaded handler, not in this constructor.
        RootGrid.Loaded += RootGrid_Loaded;
    }

    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        // Worth knowing, and not what most people assume: an AnyCPU .NET build still produces a
        // native launcher .exe whose architecture is fixed at BUILD time (it matches the machine
        // you built on). So an AnyCPU app built on x64 runs under x64 emulation on an ARM64 box --
        // it works, it is just not native. Only a Platform=ARM64 / -r win-arm64 build is.
        // If Process and OS disagree below, you are being emulated.
        var proc = RuntimeInformation.ProcessArchitecture;
        var os = RuntimeInformation.OSArchitecture;
        var emulated = proc != os ? "  (EMULATED)" : "  (native)";
        Status.Text = $"Loaded. Process: {proc}   OS: {os}{emulated}";
    }

    private void ClickMe_Click(object sender, RoutedEventArgs e)
    {
        _clicks++;
        Headline.Text = _clicks == 1 ? "Clicked once" : $"Clicked {_clicks} times";
    }

    private void ResizeAndCenter(int width, int height)
    {
        AppWindow.Resize(new SizeInt32(width, height));

        // DisplayArea.GetFromWindowId is the supported way to find the monitor the window
        // opened on; centering against the primary display is wrong on multi-monitor setups.
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);
        if (area is null) return;

        var centered = area.WorkArea;
        AppWindow.Move(new PointInt32(
            centered.X + (centered.Width - width) / 2,
            centered.Y + (centered.Height - height) / 2));
    }
}

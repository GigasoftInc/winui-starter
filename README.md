# WinUI Starter

**A WinUI desktop app that actually runs when you double-click it.**
Unpackaged, .NET 10, no MSIX, no `Class not registered`, and 36 MB published instead of 77 MB.

Public domain ([Unlicense](LICENSE)) - copy it, ship it, sell it, no attribution needed.

```
git clone https://github.com/GigasoftInc/winui-starter
```
Open `WinUIStarter.sln` in Visual Studio 2026, press F5. Then go build your app.

---

## Why this exists

Microsoft's WinUI project templates are aimed at a Store app that ships the whole Windows App SDK.
If you just want a normal Windows desktop program, several of their defaults are actively wrong, and
you find out one crash at a time. This starter is the same project with those defaults corrected, and
every correction commented in place so you can see *why* rather than cargo-cult it.

| Stock template | This starter | Why |
|---|---|---|
| `net8.0-windows` | `net10.0-windows10.0.19041.0` | The templates still start you on .NET 8. |
| Packaged (MSIX identity) | `<WindowsPackageType>None</WindowsPackageType>` | The .exe runs from Explorer instead of crashing. |
| "(Unpackaged)" launch profile | (removed - the csproj property is the real switch) | The profile *looks* like it makes you unpackaged. It doesn't. |
| `Microsoft.WindowsAppSDK` (umbrella) | `.WinUI` + `.Runtime` components | Drops 41 MB of ONNX/DirectML from your publish output. |
| `PublishTrimmed=True` on Release | trimming off | Trimming breaks WinUI XAML at run time. |
| `SelfContained=true` in the .pubxml | your call, deliberately | The template decides this for you, in a different file. |
| `x86;x64;ARM64` | `x64;ARM64` | x86 is dead weight for a new desktop app. |
| `Package.appxmanifest` + Store logos | removed | Not needed once unpackaged. |
| `Microsoft.Windows.SDK.BuildTools` | removed | Not needed here; builds clean without it. |
| (nothing) | window sizing + centering | `Window` has no `Width`/`Height`. |

Roughly 20 lines of project file instead of 60, and the first build is already correct.

Every landmine below is one this starter has already stepped on for you.

---

## Why does my WinUI app crash with "Class not registered" (REGDB_E_CLASSNOTREG)?

Because your project is **packaged** and you launched the raw `.exe`.

```
System.Runtime.InteropServices.COMException (0x80040154): Class not registered
   at WinRT.ActivationFactory.Get(String typeName)
   at Microsoft.Windows.ApplicationModel.WindowsAppRuntime.DeploymentManagerCS.AutoInitialize...
```

A packaged app only runs with **package identity**. Visual Studio hands it identity when you press
F5, so it works in the IDE - and then you double-click the exe in `bin\...\AppX\` and it dies. Nothing
is wrong with your code. This is the single most common wall people hit moving from packaged to
unpackaged.

**Fix:** one line in the `.csproj`.

```xml
<WindowsPackageType>None</WindowsPackageType>
```

That switches you to the Windows App SDK bootstrapper, which finds the runtime at startup. This
starter ships with it set.

### The "(Unpackaged)" launch profile does not make your app unpackaged

This is the part that wastes an afternoon. The template's `Properties\launchSettings.json` gives you two
profiles, and the Debug dropdown cheerfully offers:

```
MyApp (Package)      -> commandName: MsixPackage
MyApp (Unpackaged)   -> commandName: Project
```

Picking **(Unpackaged)** only tells Visual Studio to skip the MSIX deploy step and launch the exe. It does
**not** configure your project for unpackaged deployment. Without `<WindowsPackageType>None</WindowsPackageType>`
in the `.csproj`, that profile hands you the identical `REGDB_E_CLASSNOTREG` crash - so the IDE appears to
offer the thing you need while quietly not doing it. The launch profile is a *how do I start it* setting;
`WindowsPackageType` is a *what kind of app is this* setting. You need the second one.

---

## Why is my published WinUI app 77 MB?

Because the template references the **umbrella** `Microsoft.WindowsAppSDK` metapackage, which
transitively pulls the entire Windows AI/ML stack into what you ship.

Measured on this repo. Identical project, identical code, **one line changed** in the `.csproj`:

```
dotnet publish -c Release -r win-x64 --self-contained false
```

| Package reference | Published size | `onnxruntime.dll` | `DirectML.dll` |
|---|---|---|---|
| `.WinUI` + `.Runtime` (this repo) | **36 MB** | absent | absent |
| `Microsoft.WindowsAppSDK` (umbrella) | **77 MB** | 20.7 MB | 17.8 MB |

**2.1x the download**, for a machine-learning runtime a chart/CRUD/tool app never calls. The rest of the
difference is `Microsoft.Windows.AI.MachineLearning.dll`, `System.Numerics.Tensors.dll` and the
`Microsoft.Windows.AI.*.Projection` set.

**Note where it does and does not show up.** A plain unpackaged `dotnet build` barely differs (~1 MB) --
the native ONNX/DirectML binaries arrive on **publish**, and in **packaged (MSIX/AppX) builds**, which is
why a packaged Debug build already carries them. So "it builds fine and looks small" proves nothing; look
at your publish output.

**Fix:** reference the two component packages you actually use instead.

```xml
<PackageReference Include="Microsoft.WindowsAppSDK.WinUI" Version="2.2.1" />
<PackageReference Include="Microsoft.WindowsAppSDK.Runtime" Version="2.2.0" />
```

Keep **both**: `.WinUI` is the XAML framework, `.Runtime` is the framework dependency plus the
unpackaged bootstrapper. Their version numbers can legitimately differ.

What you should *not* do is try to hand-prune the ~30 remaining C#/WinRT projection DLLs. Those are
the honest floor for a managed unpackaged WinUI app - they are listed in `deps.json` and some load
dynamically.

---

## Why does the Visual Studio WinUI template target .NET 8?

No good reason, but it does - a new WinUI app created in Visual Studio 2026 still comes out as .NET 8.
Just retarget:

```xml
<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
```

**If the target framework snaps back to .NET 8**, look at `<RuntimeIdentifiers>`. .NET 8 moved to a
portable RID graph, so the old OS-version RIDs are no longer valid, and leaving them in blocks
retargeting and fails the build with `NETSDK1083`:

```xml
<!-- wrong, legacy -->
<RuntimeIdentifiers>win10-x86;win10-x64;win10-arm64</RuntimeIdentifiers>
<!-- right -->
<RuntimeIdentifiers>win-x64;win-arm64</RuntimeIdentifiers>
```

---

## Why does my Release build break when Debug works?

Trimming. The template sets `PublishTrimmed=True` for every non-Debug configuration. WinUI resolves
XAML types by reflection, and `Microsoft.Windows.SDK.NET` / `WinRT.Runtime` emit trim warnings, so a
trimmed build can fail at run time having built perfectly. This starter leaves trimming off. If you
inherited a project that has it, turn it off:

```xml
<PublishTrimmed Condition="'$(Configuration)' != 'Debug'">False</PublishTrimmed>
```

`PublishReadyToRun` is fine and worth keeping.

**It gets worse when you actually publish.** The template also writes
`Properties\PublishProfiles\win-x64.pubxml` containing:

```xml
<SelfContained>true</SelfContained>
```

So the default **Publish** is self-contained *and* trimmed - the two settings live in different files, and
the risky one only fires on the single action you take to ship. Self-contained is a legitimate choice (it
is how you run on a machine with no .NET installed, see Deploying below); trimming on top of it is not.
Decide on self-contained deliberately, and leave trimming off either way.

---

## My third-party WinUI control renders nothing, and throws no error

If you reference a WinUI **class library** that ships XAML - any control vendor's, or your own shared
control project - it builds a **`.pri`** file next to its `.dll`. That `.pri` carries the control templates
(`Generic.xaml`) and gets merged into your app's `.pri` at build time.

If the `.pri` is not beside the DLL, the failure is uniquely awful:

- no exception, no warning, no build error
- the control's type resolves, XAML parses
- its `Loaded` event fires
- your property setters run and your breakpoints hit
- **and nothing paints**

Everything says the code is working, because the code *is* working - the control just has no template to
render with. If a WinUI control is invisible while its code plainly runs, check for the `.pri` before you
debug anything else. When referencing by path, keep them together:

```xml
<Reference Include="TheControl">
  <HintPath>.\TheControl.dll</HintPath>
</Reference>
```

A correctly built NuGet package puts the `.pri` in `lib\<tfm>\` beside the DLL. If a package omits it, that
is a packaging bug in the library - not something you can work around from your app.

---

## Where is MessageBox?

WinUI does not have one. Use `ContentDialog`, which differs from `MessageBox` in two ways that bite:

```csharp
private async void ShowMessage(string text)
{
    var dlg = new ContentDialog
    {
        Title = "Message",
        Content = text,
        CloseButtonText = "OK",
        XamlRoot = this.Content.XamlRoot   // required, or it throws
    };
    await dlg.ShowAsync();                 // async, unlike MessageBox.Show
}
```

It needs a **`XamlRoot`** and it is **async**. And note what that implies: you cannot use a `ContentDialog`
to report a startup failure that happened before XAML loaded - if the Windows App SDK bootstrap fails, the
XAML framework never came up. For that case only, P/Invoke the Win32 `MessageBox`.

---

## Where is the WinUI XAML designer?

There isn't one, in any version of Visual Studio, and there is no plan for one. The WinUI Toolbox is
empty by design - this is a platform gap, not a broken install, and it hits every third-party control
vendor equally.

Use the **XAML runtime design tools** instead: start with **F5** (not Ctrl+F5) and use **Hot Reload**,
**Live Preview**, **Live Visual Tree** and **Live Property Explorer** to change the UI while the app
runs. Type your XAML by hand; IntelliSense works fully in the editor.

---

## Why doesn't Window have a Loaded event, Width or Height?

Because a WinUI `Window` is **not** a `FrameworkElement`. It has no `Width`, `Height`, `Loaded` or
`DataContext`.

- **Size/position** go through `AppWindow` - see `ResizeAndCenter` in
  [MainWindow.xaml.cs](WinUIStarter/MainWindow.xaml.cs). Note it centers on the window's *current*
  monitor via `DisplayArea.GetFromWindowId`; centering against the primary display is wrong on
  multi-monitor setups. Sizes are physical pixels, so 1000x700 is ~667x467 at 150% scaling.
- **Loaded** belongs to the root element, or to the specific control you need to initialize.

That second point matters more than it looks. Anything that needs a realized visual tree - measuring,
native interop, a control that renders through its own swap chain - must initialize from a `Loaded`
handler, not from the window constructor. If you are initializing a third-party control, hook **that
control's** `Loaded`, not the window's or the root grid's; the control is not necessarily fully
constructed when its parent is.

---

## Deploying an unpackaged WinUI app

Unlike WinForms or WPF, an unpackaged WinUI app needs **two** runtimes on the target machine:

1. the **.NET 10 Desktop Runtime**, and
2. the **Windows App SDK Runtime**.

Ship the **whole output folder**, not just the .exe. Your dev machine has both runtimes and will
happily lie to you - test on a clean box.

To produce a build that runs on a machine with nothing installed, publish self-contained. This bundles
both runtimes into the output:

```
dotnet publish -c Release -r win-x64 --self-contained true -p:WindowsAppSDKSelfContained=true
```

Use `win-arm64` for ARM64. The output is large but genuinely standalone.

**Want a friendlier failure when the runtime is missing?** Set
`<WindowsAppSDKBootstrapInitialize>false</WindowsAppSDKBootstrapInitialize>` and call
`Bootstrap.TryInitialize` yourself at startup. Report failure with a native `MessageBox` via P/Invoke -
you cannot use a `ContentDialog`, because if the bootstrap failed the XAML framework never loaded.

---

## Is it "WinUI" or "WinUI 3"?

Both, for now. At Build 2026 Microsoft dropped the "3" - it is officially just **WinUI**, and the
rename was deliberate: they stated they have no intention of building another UI framework, and are
rewriting parts of the Windows 11 shell in native WinUI. Most existing documentation and every
search result still says "WinUI 3". Same thing.

---

## Adding a chart

The base starter has no third-party dependencies on purpose. If you need charting, the same authors
maintain [ProEssentials](https://gigasoft.com/net-chart/) - a commercial Windows charting control with
a WinUI interface that renders through Direct2D/Direct3D:

```xml
<PackageReference Include="ProEssentials.Chart.Net10.WinUI" Version="11.0.0.2" />
```

```xml
xmlns:pe="using:Gigasoft.ProEssentials"
...
<pe:PegoWinUI x:Name="Pego1" />
```

```csharp
Pego1.Loaded += (s, e) => {
    Pego1.PeString.MainTitle = "Hello World";
    Pego1.PeData.Subsets = 1;
    Pego1.PeData.Points = 4;
    Pego1.PeFunction.ReinitializeResetImage();
};
```

One gotcha worth repeating: hook the **chart's** `Loaded`, not the window's.

---

## Requirements

- Visual Studio 2026 with the **Windows application development** workload
- .NET 10 SDK
- Windows 10 1809 (17763) or later

## Keywords

WinUI starter, WinUI 3 starter, WinUI template, WinUI 3 template, unpackaged WinUI, WinUI without MSIX,
Windows App SDK starter, WinUI .NET 10, WinUI REGDB_E_CLASSNOTREG, WinUI Class not registered,
WinUI unpackaged launch profile crash, WinUI DirectML onnxruntime size, WinUI publish size,
WinUI PublishTrimmed broken, WinUI XAML designer missing, WinUI window size, Window has no Loaded event,
WinUI custom control not rendering, WinUI .pri missing, WinUI control renders nothing, WinUI MessageBox,
WinUI ContentDialog XamlRoot, WinUI 3 boilerplate, NETSDK1083 winui.

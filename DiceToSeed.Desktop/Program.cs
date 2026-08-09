using DiceToSeed.Ui;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;

namespace DiceToSeed.Desktop;

/// <summary>
/// The desktop shell: the same Razor components as the browser build, rendered in a native
/// window instead of a browser tab.
///
/// Why this exists: Blazor WebAssembly cannot load over file://, so the browser build needs a
/// local web server, which on Tails means a terminal, a python command and a port. This shell
/// removes all three.
///
/// What it does NOT change: the conversion. Rolls to words happens in DiceToSeed.Core, which
/// this project references unmodified and which is pinned by the published Coldcard and
/// SeedSigner vectors. Two shells, one derivation, so the desktop build cannot quietly
/// disagree with the browser build.
///
/// This deliberately follows the official Photino.Blazor sample's shape rather than a tidier
/// one. Top-level statements were tried first and the window came up blank, because the
/// compiler-generated entry point carries no [STAThread] and the Windows WebView needs a
/// single-threaded apartment; Photino then fell back to fetching http://localhost/, which is
/// a network call this app must never make.
/// </summary>
class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        appBuilder.Services.AddLogging();

        // Selector "app" matching the <app> element in index.html, as the official sample
        // does. A "#app" selector against a <div id="app"> left the window empty.
        appBuilder.RootComponents.Add<App>("app");

        var app = appBuilder.Build();

        app.MainWindow
            .SetTitle("dice to seed")
            .SetUseOsDefaultSize(false)
            .SetSize(1000, 900)
            .SetContextMenuEnabled(false)   // nothing to copy: the user writes the words on paper
            .SetDevToolsEnabled(false);

        // A silent failure in a key-generation tool is worse than a loud one.
        AppDomain.CurrentDomain.UnhandledException += (_, error) =>
            app.MainWindow.ShowMessage("dice to seed: unhandled error", error.ExceptionObject.ToString() ?? "unknown");

        app.Run();
    }
}

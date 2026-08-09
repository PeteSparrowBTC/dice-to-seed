using DiceToSeed.Ui;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;

// The desktop shell: the same Razor components as the browser build, rendered in a native
// window instead of a browser tab.
//
// Why this exists: Blazor WebAssembly cannot load over file://, so the browser build needs a
// local web server, which on Tails means a terminal, a python command and a port. This shell
// removes all three. It opens a window and nothing listens on any interface.
//
// What it does NOT change: the conversion. Rolls to words happens in DiceToSeed.Core, which
// this project references without modification, and which is pinned by the published Coldcard
// and SeedSigner vectors. Two shells, one derivation, so the desktop build cannot quietly
// disagree with the browser build.
var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

var app = builder.Build();

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

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using DiceToSeed.Ui;

// Deliberately minimal. The default Blazor WebAssembly template registers an HttpClient
// pointed at the host; that registration is removed here and must not come back. This app
// makes no network call of any kind, so a configured HttpClient would be a loaded gun with
// no target: see CLAUDE.md rule 6.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

await builder.Build().RunAsync();

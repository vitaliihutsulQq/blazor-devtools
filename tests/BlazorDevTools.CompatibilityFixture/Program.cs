using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorDevTools.Runtime;
using BlazorDevTools.CompatibilityFixture;
using BlazorDevTools.CompatibilityFixture.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<CaseWorkspaceService>();
builder.Services.AddBlazorDevToolsRuntime();

await builder.Build().RunAsync();

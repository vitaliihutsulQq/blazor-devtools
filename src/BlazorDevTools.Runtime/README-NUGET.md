# BlazorDevTools.Runtime

`BlazorDevTools.Runtime` connects a Blazor WebAssembly app to the Blazor DevTools browser extension.

Install it:

```bash
dotnet add package BlazorDevTools.Runtime
```

Register it in `Program.cs`:

```csharp
using BlazorDevTools.Runtime;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddBlazorDevToolsRuntime();

await builder.Build().RunAsync();
```

Load the bridge script from the package in `wwwroot/index.html`:

```html
<script src="_content/BlazorDevTools.Runtime/devtoolsBridge.js"></script>
<script src="_framework/blazor.webassembly.js"></script>
```

Choose an integration mode:

- simple mode - use an app base class that inherits `DevtoolsComponentBase` and apply it from `_Imports.razor`
- experimental mode - for large partial `.razor.cs : ComponentBase` apps, the package also carries the proxy generator automatically

Simple mode example:

```csharp
using BlazorDevTools.Runtime;

namespace MyApp;

public abstract class AppComponentBase : DevtoolsComponentBase
{
}
```

```razor
@using BlazorDevTools.Runtime
@inherits AppComponentBase
```

The browser extension must also be installed locally. See the repository README for full setup, troubleshooting, and workflow details.

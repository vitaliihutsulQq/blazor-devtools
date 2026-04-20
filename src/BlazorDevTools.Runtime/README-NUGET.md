# BlazorDevTools.Runtime

Install the runtime package into a Blazor WebAssembly app:

```bash
dotnet add package BlazorDevTools.Runtime
```

Register the runtime in `Program.cs`:

```csharp
using BlazorDevTools.Runtime;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddBlazorDevToolsRuntime();

await builder.Build().RunAsync();
```

Load the bridge script in `wwwroot/index.html`:

```html
<script src="_content/BlazorDevTools.Runtime/devtoolsBridge.js"></script>
<script src="_framework/blazor.webassembly.js"></script>
```

Recommended global tracking pattern:

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

Apply that `_Imports.razor` pattern so your components appear in the Blazor DevTools tree without per-file opt-in.

For large apps that already use many partial `.razor.cs : ComponentBase` components, the package also includes an experimental proxy/generator tracking path automatically. You do not need to add a separate analyzer package manually.

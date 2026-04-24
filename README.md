# Blazor DevTools

Lightweight developer tooling for **Blazor WebAssembly** apps.

Blazor DevTools helps you inspect a running component tree, understand why components re-render, and explore component metadata from a Chromium DevTools panel.

> [!WARNING]
> **Project status:** experimental. The recommended path today is the simple runtime integration. The generator/proxy path is available for controlled trials, but it does not yet support every component shape.

| At a glance | |
| --- | --- |
| Best first step | Run the sample app and load the unpacked extension |
| Recommended integration | Runtime package + inheritance-based component base |
| Browser support | Chromium-based browsers |
| Deeper docs | [`docs/`](docs/) |

## What you get today

- A DevTools panel with a browsable component tree, parameters, and metadata
- Search and filter by component name or full type name
- Auto-refresh snapshots after component updates
- Inspection of injected services and tracked cascading values
- Lifecycle and performance metrics such as render counts, timings, and state-change counts
- Render-cause inspection to help explain recent re-renders
- An opt-in DOM picker for components that expose explicit DOM marker attributes
- A runtime NuGet package and an in-repo sample app for quick local trials

## Quick try (30-60s)

> [!TIP]
> If you are visiting the repository for the first time, start here.

### 1) Run the sample app

```bash
dotnet run --project src/BlazorDevTools.SampleApp/BlazorDevTools.SampleApp.csproj
```

### 2) Build and load the extension

```bash
cd src/BlazorDevTools.Extension
npm ci
npm run build
```

Then open `chrome://extensions` or `edge://extensions`, enable **Developer mode**, choose **Load unpacked**, and select:

```text
src/BlazorDevTools.Extension/dist
```

### 3) Open the Blazor panel

Open DevTools on the sample app and select the **Blazor** panel. You should see the component tree and be able to inspect parameters and metadata.

## Status and limitations

- The project is under active development and should still be considered experimental.
- The simple inheritance integration is the recommended default today and is stable for small-to-medium apps.
- An experimental Roslyn generator/proxy integration exists for large code-behind-heavy apps, but it does not yet cover every component shape.
- DOM picker and page highlighting are opt-in and require explicit DOM marker attributes on elements.

## Choose an integration path

| Path | Best for | Status |
| --- | --- | --- |
| **Simple runtime integration** | Most apps, especially new or small-to-medium projects | **Recommended** |
| **Generator/proxy integration** | Large code-behind-heavy apps where changing many base classes is costly | **Experimental** |

### 1) Simple runtime integration (recommended)

1. Add the runtime package, or use a `ProjectReference` during local development.
2. Register the runtime in `Program.cs`.
3. Include the packaged bridge script in `wwwroot/index.html`.
4. (Optional) Use a shared component base that inherits `DevtoolsComponentBase` to opt in components by inheritance — this is not required for the runtime integration to work.

**Package install**

```bash
dotnet add package BlazorDevTools.Runtime
```

**Minimal setup**

The following is the required baseline for the simple runtime integration. Install the runtime package, register the runtime services, and include the bridge script in your app. No component inheritance is required for basic functionality.

`Program.cs`

```csharp
using BlazorDevTools.Runtime;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.Services.AddBlazorDevToolsRuntime();
await builder.Build().RunAsync();
```

`wwwroot/index.html`

```html
<script src="_content/BlazorDevTools.Runtime/devtoolsBridge.js"></script>
<script src="_framework/blazor.webassembly.js"></script>
```

**Optional: inheritance-based setup**

If you prefer an inheritance-based opt-in for components, you can add a shared base class that inherits DevtoolsComponentBase and apply it via `_Imports.razor`. This is optional — the runtime integration works without this.

`AppComponentBase.cs` (optional)

```csharp
using BlazorDevTools.Runtime;

public abstract class AppComponentBase : DevtoolsComponentBase
{
}
```

`_Imports.razor` (optional)

```razor
@inherits AppComponentBase
```

> [!NOTE]
> For package installation notes, private feed setup, and concise consumer examples, see [`src/BlazorDevTools.Runtime/README-NUGET.md`](src/BlazorDevTools.Runtime/README-NUGET.md).

### 2) Extension setup

To use Blazor DevTools with your own app, the browser extension must also be installed locally:

```bash
cd src/BlazorDevTools.Extension
npm ci
npm run build
```

Load the generated `src/BlazorDevTools.Extension/dist` folder as an unpacked extension in a Chromium-based browser.

### 3) Experimental generator/proxy mode

> [!IMPORTANT]
> This path is intended for controlled trials. It is not a drop-in replacement for every Blazor component shape.

- Purpose: reduce the need to change many existing partial `.razor.cs` base classes.
- How it works: the runtime delivers an analyzer/generator; eligible components receive generated proxy types and a manifest, and the runtime activates proxies at runtime.
- Known limitations: some component shapes are intentionally skipped, including sealed, generic, nested, and abstract scenarios.
- Details and eligibility rules: [`docs/experimental-proxy-integration.md`](docs/experimental-proxy-integration.md)

## Repository guide

| Area | Purpose |
| --- | --- |
| `src/BlazorDevTools.Runtime` | Runtime library and static web-asset bridge |
| `src/BlazorDevTools.Protocol` | Shared protocol contracts |
| `src/BlazorDevTools.Generators` | Experimental Roslyn generator/analyzer |
| `src/BlazorDevTools.Extension` | Chromium extension UI |
| `src/BlazorDevTools.SampleApp` | Small sample app for local trials |
| `tests/` and `artifacts/` | Tests, compatibility fixture, and package validation helpers |
| `docs/` | Troubleshooting, publishing, and deeper design notes |

## Build, validate, and troubleshoot

### Build and test

```bash
dotnet restore BlazorDevTools.sln
dotnet build BlazorDevTools.sln
dotnet test BlazorDevTools.sln
```

### Build the extension

```bash
cd src/BlazorDevTools.Extension
npm ci
npm run build
```

### Pack local packages

```bash
dotnet pack src/BlazorDevTools.Protocol/BlazorDevTools.Protocol.csproj -c Release -o artifacts/local-packages
dotnet pack src/BlazorDevTools.Runtime/BlazorDevTools.Runtime.csproj -c Release -o artifacts/local-packages
```

### Validation notes

- Use `src/BlazorDevTools.SampleApp` and `tests/BlazorDevTools.ExternalConsumer` to verify the simple runtime flow.
- For generator/proxy validation, use `tests/BlazorDevTools.CompatibilityFixture` and `artifacts/package-consumer-validation`.

### Troubleshooting and deeper docs

- [`docs/troubleshooting.md`](docs/troubleshooting.md)
- [`docs/experimental-proxy-integration.md`](docs/experimental-proxy-integration.md)
- [`docs/compatibility-fixture.md`](docs/compatibility-fixture.md)
- [`docs/publishing.md`](docs/publishing.md)

## Contributing

PRs are welcome. Please keep changes focused, add tests for behavior changes when reasonable, and update documentation when changing integration flows.

## License

This repository is licensed under the [MIT License](LICENSE). Package artifacts declare the same license in project metadata.

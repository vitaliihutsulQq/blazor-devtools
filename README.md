# Blazor DevTools

Blazor DevTools is an experimental developer tool for Blazor WebAssembly apps inspired by Angular DevTools.

The repository currently contains:

- `src/BlazorDevTools.Runtime` - runtime integration package for Blazor apps
- `src/BlazorDevTools.Extension` - browser extension scaffold and panel UI
- `src/BlazorDevTools.SampleApp` - in-repo sample app used during development
- `tests/BlazorDevTools.ExternalConsumer` - separate consuming app that validates the install flow

## Install The Extension

1. From `src/BlazorDevTools.Extension`, run `npm install`
2. From `src/BlazorDevTools.Extension`, run `npm run build`
3. In a Chromium-based browser, open the extensions page and enable developer mode
4. Load the unpacked extension from `src/BlazorDevTools.Extension/dist`
5. Open browser devtools and select the `Blazor` panel

## Install The Runtime In A Blazor WASM App

Add a package or project reference to `BlazorDevTools.Runtime`.

For local development in this repo, the consuming app uses:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\BlazorDevTools.Runtime\BlazorDevTools.Runtime.csproj" />
</ItemGroup>
```

You do not need to reference `BlazorDevTools.Protocol` directly. `BlazorDevTools.Runtime` brings it along as an implementation detail.

## Pack And Publish The Runtime

Create a local NuGet package with:

```bash
dotnet pack src/BlazorDevTools.Runtime/BlazorDevTools.Runtime.csproj -c Release
```

This produces a `.nupkg` and `.snupkg` under `src/BlazorDevTools.Runtime/bin/Release`.

Before publishing to GitHub Packages, make sure:

1. The package metadata URLs in `src/BlazorDevTools.Runtime/BlazorDevTools.Runtime.csproj` point at the real repository
2. Your GitHub Packages source and credentials are configured locally
3. Any transitive internal packages required by the runtime are available in the target feed

## GitHub Actions Publish Flow

- CI workflow: `.github/workflows/ci.yml` runs on pull requests and pushes to `main`, and only builds/tests the solution plus builds the browser extension
- Publish workflow: `.github/workflows/publish-packages.yml` runs only on manual dispatch or version tags like `v0.1.0`
- GitHub Packages authentication uses the workflow `GITHUB_TOKEN`; no personal access token is hardcoded into the repo
- Publishing order is enforced in the workflow by packing and publishing `BlazorDevTools.Protocol` first, then `BlazorDevTools.Runtime`

Before the publish workflow can succeed in GitHub, make sure the repository and package settings allow workflow-based package publishing with `GITHUB_TOKEN`.

## Program.cs Setup

Register the runtime services in `Program.cs`:

```csharp
using BlazorDevTools.Runtime;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddBlazorDevToolsRuntime();

await builder.Build().RunAsync();
```

## Global Tracking Mode

The recommended integration pattern is to enable tracking across your component project through `_Imports.razor`.

Create an app-level base class:

```csharp
using BlazorDevTools.Runtime;

namespace MyApp;

public abstract class AppComponentBase : DevtoolsComponentBase
{
}
```

Then opt components into tracking globally:

```razor
@using BlazorDevTools.Runtime
@inherits AppComponentBase
```

With this pattern in `_Imports.razor`, regular Razor components and pages appear in the DevTools tree without adding `@inherits DevtoolsComponentBase` one file at a time.

This is the recommended discoverability-first mode: install the runtime, open the `Blazor` panel, and discover tracked components immediately.

## index.html Script Setup

Load the runtime bridge from the packaged static web asset:

```html
<script src="_content/BlazorDevTools.Runtime/devtoolsBridge.js"></script>
<script src="_framework/blazor.webassembly.js"></script>
```

## Tracked Component Setup

Recommended pattern:

1. Use global tracking through `_Imports.razor` with `@inherits AppComponentBase`
2. Apply `@attributes="DevToolsMarkerAttributes"` to a component's visible root element when you want DOM picker and page highlight support
3. Cascade `ComponentId` into tracked child components with `ParentComponentCascadeName` when you build explicitly nested tracked regions

Example:

```razor
<section class="card" @attributes="DevToolsMarkerAttributes">
    <h3>@Title</h3>

    <CascadingValue Name="@ParentComponentCascadeName" Value="ComponentId" IsFixed="true">
        <TrackedChild Value="@Count" />
    </CascadingValue>
</section>

@code {
    [Parameter]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public int Count { get; set; }
}
```

Tree tracking does not require DOM markers. DOM markers are optional and only power `Pick From Page`, highlight overlays, and DOM-to-component mapping.

## Custom Base Classes

Global tracking through `_Imports.razor` only applies to components that do not already declare their own base class.

For components with custom base classes, make those base classes inherit from your app-level base when practical:

```csharp
public abstract class ReportComponentBase : AppComponentBase
{
}
```

This is the recommended incremental path for existing apps. Layouts or components that must continue using another explicit base type may stay untracked until you introduce a tracked intermediate base for them.

## Verify The Connection

1. Start your Blazor WASM app
2. Open the page in a Chromium-based browser with the extension loaded
3. Open browser devtools and switch to the `Blazor` panel
4. Confirm the component tree appears automatically without manually preparing individual components
5. Use the tree search box to find components by name or full type name
6. Toggle `Pick From Page` in the panel, hover a DOM-marked tracked element, and click it
7. Confirm the matching component is selected in the tree and details pane

For a working reference, use `tests/BlazorDevTools.ExternalConsumer`, which follows the same setup from a separate consuming app.

# Blazor DevTools

Blazor DevTools is an experimental developer tool for Blazor WebAssembly applications inspired by Angular DevTools.

Today the repository contains two cooperating pieces:

- `src/BlazorDevTools.Extension` - a Chromium browser extension that renders the component tree, details pane, search/filter UI, and optional page picker
- `src/BlazorDevTools.Runtime` - a runtime package that publishes component snapshots from a Blazor WebAssembly app to the extension

The project currently supports two tracking modes:

- simple inheritance-based mode - stable and easy for small or greenfield apps
- experimental generator/proxy mode - intended for large code-behind-heavy apps that cannot adopt a global `@inherits` pattern

This repository also contains validation apps and fixtures so the tracking modes can be exercised without a separate private consumer repository.

## Current Status

- The extension can show a component tree, search/filter components, show metadata, auto-refresh, and map DOM-marked elements back to components
- `BlazorDevTools.Runtime` is NuGet-packaged and includes the static web asset bridge script
- The experimental generator path is packaged through `BlazorDevTools.Runtime` and can be tried in real apps, but it is still not the primary recommended integration mode for every codebase
- DOM picker support remains opt-in even when generator-based tracking is enabled
- Large-app compatibility is promising, but still provisional for advanced component shapes

## Repository Structure

- `src/BlazorDevTools.Protocol` - shared protocol contracts used by the runtime and extension
- `src/BlazorDevTools.Generators` - Roslyn source generator for the experimental proxy-based integration path
- `src/BlazorDevTools.Runtime` - runtime package for Blazor WebAssembly apps, including static web assets and experimental analyzer delivery
- `src/BlazorDevTools.SampleApp` - local sample app used for everyday runtime and extension development
- `tests/BlazorDevTools.Runtime.Tests` - runtime behavior tests
- `tests/BlazorDevTools.Generators.Tests` - source generator tests
- `tests/BlazorDevTools.CompatibilityFixture` - large-app compatibility fixture with representative `.razor` + `.razor.cs` patterns
- `tests/BlazorDevTools.CompatibilityFixture.Tests` - tests that verify generator behavior against the compatibility fixture
- `tests/BlazorDevTools.ExternalConsumer` - separate consumer-style app for the simple inheritance-based install flow
- `artifacts/package-consumer-validation` - package-based validation app for the experimental generator path
- `.github/workflows` - CI and GitHub Packages publishing workflows
- `docs/` - internal design notes, troubleshooting, publishing guidance, and validation notes

## Build And Test

### .NET

```bash
dotnet restore BlazorDevTools.sln
dotnet build BlazorDevTools.sln
dotnet test BlazorDevTools.sln
```

Useful narrower commands:

```bash
dotnet test tests/BlazorDevTools.Runtime.Tests/BlazorDevTools.Runtime.Tests.csproj
dotnet test tests/BlazorDevTools.Generators.Tests/BlazorDevTools.Generators.Tests.csproj
dotnet test tests/BlazorDevTools.CompatibilityFixture.Tests/BlazorDevTools.CompatibilityFixture.Tests.csproj
```

### Browser Extension

```bash
cd src/BlazorDevTools.Extension
npm ci
npm run build
```

### Package Build

```bash
dotnet pack src/BlazorDevTools.Protocol/BlazorDevTools.Protocol.csproj -c Release
dotnet pack src/BlazorDevTools.Runtime/BlazorDevTools.Runtime.csproj -c Release
```

## Browser Extension

### Local Install

1. Build the extension:
   ```bash
   cd src/BlazorDevTools.Extension
   npm ci
   npm run build
   ```
2. Open the extensions page in a Chromium-based browser
3. Enable developer mode
4. Load the unpacked extension from `src/BlazorDevTools.Extension/dist`
5. Open browser devtools on a Blazor WASM app and select the `Blazor` panel

### What The Extension Currently Does

- receives component tree snapshots from the inspected Blazor app
- shows a tree view with expand/collapse
- supports search by component name or full type name
- shows component metadata and parameters in a details pane
- supports auto-refresh after component changes
- supports optional `Pick From Page` for DOM-marked tracked components

## Installing The Runtime Package

### From GitHub Packages

Add a GitHub Packages NuGet source first.

```bash
dotnet nuget add source "https://nuget.pkg.github.com/vitaliihutsulQq/index.json" \
  --name "github-blazordevtools" \
  --username "YOUR_GITHUB_USERNAME" \
  --password "YOUR_GITHUB_PAT" \
  --store-password-in-clear-text
```

PAT guidance for consumers:

- required: `read:packages`
- if the repository or packages are private: also grant whatever repository access is needed to read that private package source, typically `repo`

Then install the runtime package:

```bash
dotnet add package BlazorDevTools.Runtime --version 0.1.3
```

Consumers do not need to install `BlazorDevTools.Protocol` manually. `BlazorDevTools.Runtime` carries the dependency and also delivers the experimental proxy generator analyzer automatically.

### Local Development Via Project Reference

For local work inside this repository, consuming apps can reference the runtime project directly:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\BlazorDevTools.Runtime\BlazorDevTools.Runtime.csproj" />
</ItemGroup>
```

Use the packaged flow when validating automatic analyzer delivery. A plain project reference is convenient for repo development, but it does not model NuGet analyzer delivery exactly the same way.

## Runtime Setup

### `Program.cs`

Register the runtime services:

```csharp
using BlazorDevTools.Runtime;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddBlazorDevToolsRuntime();

await builder.Build().RunAsync();
```

### `wwwroot/index.html`

Load the packaged bridge script:

```html
<script src="_content/BlazorDevTools.Runtime/devtoolsBridge.js"></script>
<script src="_framework/blazor.webassembly.js"></script>
```

That script is required for:

- initial snapshot handshake when the panel opens after the page is already loaded
- ongoing runtime-to-extension snapshot delivery

## Integration Modes

## Simple Inheritance-Based Mode

### Who it is for

- small to medium Blazor WASM apps
- apps that can standardize on a shared tracked base class
- greenfield apps or apps with few custom component bases

### How it works

Create an app-level base class:

```csharp
using BlazorDevTools.Runtime;

namespace MyApp;

public abstract class AppComponentBase : DevtoolsComponentBase
{
}
```

Then apply it through `_Imports.razor`:

```razor
@using BlazorDevTools.Runtime
@inherits AppComponentBase
```

### Benefits

- simplest setup
- stable and explicit
- full access to current tree tracking behavior
- works well with DOM marker helpers like `DevToolsMarkerAttributes`

### Limitations

- breaks down in large code-behind-heavy apps where many `.razor.cs` files already inherit from `ComponentBase` or another base
- `_Imports.razor @inherits` causes partial-class base conflicts in those apps

### DOM Picker Expectations

- tree tracking works without DOM markers
- `Pick From Page` still requires explicit DOM marker usage, typically `@attributes="DevToolsMarkerAttributes"`

### Maturity

- recommended default mode today

## Experimental Generator / Proxy Mode

### Who it is for

- large Blazor WASM apps with many partial `.razor.cs : ComponentBase` components
- apps where mass-changing base classes is not realistic

### How it works

- `BlazorDevTools.Runtime` delivers the analyzer/generator automatically when installed from NuGet
- eligible components get generated proxy subclasses and a generated manifest
- the runtime activator swaps the original component type for the proxy when a generated mapping exists

### Benefits

- avoids `_Imports.razor @inherits DevtoolsComponentBase`
- avoids mass-editing hundreds of code-behind partial classes
- preserves the current runtime architecture and extension flow

### Current MVP Eligibility

- non-abstract
- non-sealed
- non-generic
- partial classes
- top-level classes
- derives from `ComponentBase`
- not already tracked through `DevtoolsComponentBase`
- not already implementing `IDevToolsComponentProxy`
- not in `Microsoft.AspNetCore*`

### Limitations

- still experimental
- does not cover every enterprise component shape yet
- skips sealed, generic, nested, abstract, and already-tracked components
- DOM picker support is still opt-in and separate from this mode
- should be trialed against the app with diagnostics enabled before broad rollout

### DOM Picker Expectations

- tree snapshots, parameter capture, and auto-refresh can work without DOM markers
- DOM picker and page highlight still need explicit DOM markers on visible elements

### Maturity

- ready for controlled trials, not yet the primary recommendation for every consumer app

## What Not To Do In Large Partial-Class Apps

Do not apply `_Imports.razor @inherits AppComponentBase` blindly in a codebase where many `.razor.cs` files already declare:

```csharp
public partial class SomeComponent : ComponentBase
```

That causes the classic partial-class base conflict:

- Razor-generated partial uses `AppComponentBase`
- code-behind partial uses `ComponentBase`
- the project fails to compile because partial declarations cannot use different base classes

Use the experimental generator/proxy path for that style of codebase instead.

## Verification Flows

### Simple Mode Verification

Use `tests/BlazorDevTools.ExternalConsumer` as the reference app.

```bash
dotnet run --project tests/BlazorDevTools.ExternalConsumer/BlazorDevTools.ExternalConsumer.csproj
```

Verify:

1. the app loads with the extension enabled
2. the `Blazor` panel shows a tree automatically
3. search/filter works
4. `Pick From Page` works on DOM-marked elements

### Experimental Generator Mode Verification

Use these validation assets:

- `tests/BlazorDevTools.CompatibilityFixture` - representative in-repo shape coverage
- `artifacts/package-consumer-validation` - package-based validation for automatic analyzer delivery

Compatibility fixture:

```bash
dotnet run --project tests/BlazorDevTools.CompatibilityFixture/BlazorDevTools.CompatibilityFixture.csproj
```

Package-based validation:

1. Pack local packages:
   ```bash
   dotnet pack src/BlazorDevTools.Protocol/BlazorDevTools.Protocol.csproj -c Release -o artifacts/local-packages
   dotnet pack src/BlazorDevTools.Runtime/BlazorDevTools.Runtime.csproj -c Release -o artifacts/local-packages
   ```
2. Build the validation app:
   ```bash
   dotnet restore artifacts/package-consumer-validation/PackageConsumerValidation.csproj
   dotnet build artifacts/package-consumer-validation/PackageConsumerValidation.csproj
   ```

The package validation project compiles against generated proxy types. If the generator is not delivered through the runtime package, that build fails.

## Troubleshooting

For detailed troubleshooting notes, see `docs/troubleshooting.md`.

Common issues:

- panel says it is waiting for a snapshot
- extension loads but the tree is empty
- GitHub Packages authentication fails during restore
- generator skips components unexpectedly
- partial-class base conflicts after adding `_Imports.razor @inherits`
- package-based generator delivery does not seem to activate

## CI And Package Publishing

### CI Workflow

- file: `.github/workflows/ci.yml`
- trigger: pull requests and pushes to `main`
- purpose: validate build, tests, and extension build only
- CI currently authenticates to GitHub Packages for restore with `GH_PACKAGES_TOKEN`
- no package publishing happens here

### Publish Workflow

- file: `.github/workflows/publish-packages.yml`
- trigger: `workflow_dispatch` and tag pushes like `v0.1.3`
- purpose: build, test, pack, and publish packages to GitHub Packages
- authentication: `GITHUB_TOKEN`
- publish order is enforced:
  1. `BlazorDevTools.Protocol`
  2. `BlazorDevTools.Runtime`

For a fuller release walkthrough, see `docs/publishing.md`.

### Updating A Consumer App To A New Version

1. publish or obtain the new package version
2. update the consumer reference:
   ```bash
   dotnet add package BlazorDevTools.Runtime --version 0.1.3
   ```
3. restore and rebuild the app
4. if using the extension locally, rebuild/reload it if extension-side behavior also changed

## Related Documentation

- `src/BlazorDevTools.Runtime/README-NUGET.md` - concise runtime package install instructions
- `docs/experimental-proxy-integration.md` - internal generator/proxy architecture notes
- `docs/compatibility-fixture.md` - what the compatibility fixture covers
- `docs/troubleshooting.md` - practical trial and install troubleshooting
- `docs/publishing.md` - package publishing and release workflow notes

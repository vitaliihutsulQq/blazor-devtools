# Blazor DevTools

Lightweight developer tooling for Blazor WebAssembly apps — component-tree inspection, parameter & metadata details, component search, and an optional DOM picker.

This project is experimental. It helps developers understand and debug component trees in Blazor WASM apps by shipping a small runtime library and a Chromium DevTools extension. The extension shows component snapshots published by the runtime from the running app.

What you get today
- A Chromium-based DevTools panel with a browsable component tree, parameters, and metadata
- Search / filter components by name or full type name
- Auto-refresh snapshots after component updates
- Inspect injected services and tracked cascading values for components
- Lifecycle and performance metrics (render counts, timings, state changes)
- Render-cause inspection (why a component recently re-rendered)
- An opt-in DOM picker that highlights components which expose explicit DOM marker attributes
- A runtime NuGet package (BlazorDevTools.Runtime) and an in-repo sample app for quick trials

Quick try (30–60s)
1. Build and run the sample app:

   dotnet run --project src/BlazorDevTools.SampleApp/BlazorDevTools.SampleApp.csproj

2. Build the extension and load it unpacked in a Chromium browser:

   cd src/BlazorDevTools.Extension
   npm ci
   npm run build

   Then open chrome://extensions (or edge://extensions), enable Developer mode and Load unpacked → select src/BlazorDevTools.Extension/dist

3. Open DevTools on the sample app and choose the "Blazor" panel. You should see a component tree and be able to explore parameters.

Status and limitations (honest)
- Project is experimental and under active development.
- The simple inheritance integration is the recommended default today and is stable for small-to-medium apps.
 - An experimental Roslyn generator/proxy integration exists for large code-behind-heavy apps; it is usable for controlled trials but does not yet cover every component shape.
 - DOM picker and page highlighting are opt-in and require explicit DOM marker attributes on elements.

Repository overview (high level)
- src/BlazorDevTools.Extension — Chromium extension UI (TypeScript)
- src/BlazorDevTools.Runtime — runtime library and static web-asset bridge (NuGet package)
- src/BlazorDevTools.Protocol — shared protocol contracts
- src/BlazorDevTools.Generators — experimental Roslyn generator/analyzer
- src/BlazorDevTools.SampleApp — small sample app to try the extension
- tests/ and artifacts/ — unit tests, compatibility fixture, and package validation helpers
- docs/ — deeper, developer-focused documentation (publishing, troubleshooting, compatibility notes)

If you want deep developer or release guidance, see docs/ (links at the end).

Getting started for integrators
1) Simple (recommended default)
- Add the runtime package or reference the project in development.
- Register services in Program.cs and include the packaged bridge script in index.html.

Minimal runtime setup (conceptual)

Program.cs:

using BlazorDevTools.Runtime;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.Services.AddBlazorDevToolsRuntime();
await builder.Build().RunAsync();

index.html:

<script src="_content/BlazorDevTools.Runtime/devtoolsBridge.js"></script>
<script src="_framework/blazor.webassembly.js"></script>

2) Experimental generator/proxy (for large code-behind apps)
- Purpose: avoid changing many existing partial `.razor.cs` base classes.
- How it works (short): the runtime delivers an analyzer/generator; eligible components get generated proxy types and a manifest; the runtime activates proxies at runtime.
- Status: usable for controlled trials. It is not a silver bullet and skips some component shapes (sealed, generic, nested, abstract, etc.). See docs/experimental-proxy-integration.md for details and eligibility rules.

Packaging and installing the runtime
 - Local development: reference the runtime project directly with a ProjectReference for fast iteration.
 - Package consumers: the runtime is distributed as a NuGet package. When using GitHub Packages or another private feed, add the NuGet source and follow the provider's authentication instructions (for GitHub Packages this commonly means a PAT with read:packages). See src/BlazorDevTools.Runtime/README-NUGET.md for concise install notes and examples.
 - Do not hardcode version examples in consumer docs here; check Releases or the package feed for the current version.

Developer: build & test (short)
- Restore / build / test the solution:

  dotnet restore BlazorDevTools.sln
  dotnet build BlazorDevTools.sln
  dotnet test BlazorDevTools.sln

- Build the extension: cd src/BlazorDevTools.Extension && npm ci && npm run build
- Pack local packages if you need package-based validation:

  dotnet pack src/BlazorDevTools.Protocol/BlazorDevTools.Protocol.csproj -c Release -o artifacts/local-packages
  dotnet pack src/BlazorDevTools.Runtime/BlazorDevTools.Runtime.csproj -c Release -o artifacts/local-packages

Verification & validation notes
- Use src/BlazorDevTools.SampleApp and tests/BlazorDevTools.ExternalConsumer to verify the simple inheritance flow.
- For generator/proxy validation, see tests/BlazorDevTools.CompatibilityFixture and artifacts/package-consumer-validation. See docs/compatibility-fixture.md and docs/experimental-proxy-integration.md.

Troubleshooting and deeper docs
- docs/troubleshooting.md — common issues and guidance
- docs/experimental-proxy-integration.md — architecture, eligibility, and limitations of the generator path
- docs/publishing.md — release and package publishing workflow

CI and publishing (brief)
- CI validates build, tests, and extension artifacts on PRs. See .github/workflows/ci.yml.
- Package publishing is handled by .github/workflows/publish-packages.yml and publishes Protocol first, then Runtime. See docs/publishing.md for the release walkthrough.

Contributing
- PRs are welcome. Please keep changes focused, add tests for behavior changes where reasonable, and update docs when adding or changing integration flows.

License
- Package artifacts (Protocol and Runtime) declare the MIT license in their project metadata (see PackageLicenseExpression in src/BlazorDevTools.Protocol/BlazorDevTools.Protocol.csproj and src/BlazorDevTools.Runtime/BlazorDevTools.Runtime.csproj).
- This repository includes a top-level LICENSE file matching the package metadata (MIT). See LICENSE in the repository root for the full text.

Related files
- src/BlazorDevTools.Runtime/README-NUGET.md — concise runtime package install instructions
- docs/ — full developer and release documentation

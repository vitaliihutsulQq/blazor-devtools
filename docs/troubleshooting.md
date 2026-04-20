# Troubleshooting

## No Snapshot Appears In The Panel

Check these in order:

1. Confirm the extension is loaded from `src/BlazorDevTools.Extension/dist`
2. Confirm the app includes:
   - `builder.Services.AddBlazorDevToolsRuntime()`
   - `<script src="_content/BlazorDevTools.Runtime/devtoolsBridge.js"></script>`
3. Reload the app page after enabling or reloading the extension
4. Open browser devtools and switch to the `Blazor` panel

If the tree is still empty, see the next sections.

## Extension Installed But Tree Is Empty

Common causes:

- the runtime package is missing or not registered in `Program.cs`
- the bridge script is missing from `index.html`
- the panel opened before the content script was available and the page was not reloaded
- the app has no tracked components under the current integration mode

Check integration mode next:

- simple mode requires `DevtoolsComponentBase` through an app base class and `_Imports.razor`
- experimental mode requires generator-eligible partial `ComponentBase` components and a package-based runtime install

## GitHub Packages Restore Or Auth Fails

Consumers need a GitHub Packages source configured locally:

```bash
dotnet nuget add source "https://nuget.pkg.github.com/vitaliihutsulQq/index.json" \
  --name "github-blazordevtools" \
  --username "YOUR_GITHUB_USERNAME" \
  --password "YOUR_GITHUB_PAT" \
  --store-password-in-clear-text
```

PAT guidance:

- required: `read:packages`
- often also required for private repositories: `repo`

If restore still fails:

- confirm the source URL exactly matches the package feed
- confirm your PAT is still valid
- remove and re-add the source if necessary
- check whether your account has access to the repository and package feed

## Partial Class Base-Class Conflicts

Symptom:

- `Partial declarations of 'X' must not specify different base classes`

Cause:

- `_Imports.razor` applies `@inherits AppComponentBase`
- a `.razor.cs` partial already declares `: ComponentBase` or another base type

Fix:

- do not use the simple inheritance mode for those components
- use the experimental generator/proxy path instead

## Generator Skipped Components

The experimental generator emits info diagnostics with IDs `BDTG001` to `BDTG009`.

Typical examples:

- `BDTG002` - sealed component
- `BDTG003` - generic component
- `BDTG005` - already tracked by inheritance mode
- `BDTG009` - non-partial component

Use the diagnostic message to decide whether to:

- leave the component untracked for now
- move it to the simple mode through a wrapper or custom base
- refactor the component shape if tracking is important

## Namespace Mismatch Or Generated Proxy Type Errors

Symptoms can include:

- `The type or namespace name 'X__BlazorDevToolsProxy' does not exist in the namespace ...`
- follow-up metadata-file-not-found errors because the project failed to compile

Current status:

- the manifest/proxy namespace mismatch bug has been fixed
- generator references now use the actual Roslyn symbol namespace rather than string substitution on the full type name

If a similar error appears again:

- inspect generated files under `obj/Generated`
- compare the proxy declaration namespace to the manifest registration namespace
- report the exact generated type pair that disagrees

## Extension Reload Or Build Issues

Rebuild the extension with:

```bash
cd src/BlazorDevTools.Extension
npm ci
npm run build
```

Then reload the unpacked extension in the browser.

If the panel still behaves oddly:

- close and reopen browser devtools
- reload the inspected app tab
- make sure `dist` is the folder loaded by the browser, not `src`

## Experimental Generator Path Does Not Seem To Activate

Important distinction:

- package install is the authoritative validation path for automatic analyzer delivery
- a plain project reference to `BlazorDevTools.Runtime.csproj` is convenient for repo development, but it does not mirror NuGet analyzer behavior exactly

Use:

- `artifacts/package-consumer-validation`
- `tests/BlazorDevTools.CompatibilityFixture`

to validate generator-based behavior before trialing in a large real app.

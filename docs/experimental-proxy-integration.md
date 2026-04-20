# Experimental Proxy Integration

## Why The Inheritance-First Model Fails In Large Apps

The simple integration path uses `DevtoolsComponentBase` as the tracked component base type.

That works for small apps, but it breaks down in large Blazor WebAssembly projects where many components use code-behind files such as `Component.razor.cs` and explicitly inherit from `ComponentBase` or another custom base class.

If `_Imports.razor` applies `@inherits AppComponentBase`, the Razor-generated partial class and the code-behind partial class end up declaring different base classes for the same component type. C# rejects that partial type combination.

## Proxy + Activator Path

The experimental path avoids changing the original component type.

- The original component remains unchanged
- A proxy subtype inherits from the original component
- `IComponentActivator` swaps the original type for the proxy only when a mapping exists
- The proxy performs tracking through shared runtime lifecycle helpers

This is additive to the inheritance-based path. Components that already use `DevtoolsComponentBase` continue to work the same way.

## Generator-Ready Runtime Shape

The runtime now exposes a small set of abstractions a future source generator can target:

- `IDevToolsComponentProxy`
  - marks a proxy as a Blazor DevTools proxy
  - exposes the original component type through `DevToolsOriginalComponentType`
- `DevToolsTrackedComponentLifecycle`
  - owns component id generation
  - registers the component with `ComponentTracker`
  - updates parameters
  - increments render count
  - unregisters on disposal
  - wraps child content in a parent-id `CascadingValue`
- `DevToolsParameterSnapshotFactory`
  - captures serializable parameter snapshots before any async boundary
- `IDevToolsComponentProxyRegistry`
  - resolves original component type to proxy type
- `DevToolsComponentActivator`
  - swaps to the proxy only when a registry mapping exists

## Generator MVP Scope

The first generator MVP is now implemented in `src/BlazorDevTools.Generators`.

Current eligibility rules:

- include non-abstract, non-sealed, non-generic partial classes
- include only classes that inherit from `Microsoft.AspNetCore.Components.ComponentBase`
- skip nested classes
- skip classes that already inherit from `DevtoolsComponentBase`
- skip classes that already implement `IDevToolsComponentProxy`
- skip framework-style namespaces under `Microsoft.AspNetCore`

For eligible components, the generator emits:

- a proxy subtype named `<ComponentName>__BlazorDevToolsProxy`
- a generated manifest implementing `IDevToolsComponentProxyManifest`
- lifecycle interception code built on `DevToolsTrackedComponentLifecycle`

The runtime discovers the generated manifest automatically by scanning loaded assemblies for `IDevToolsComponentProxyManifest` implementations.

## Generator Diagnostics

The generator now emits info diagnostics for skipped component declarations so large-app trials can see why a component was not proxied.

| ID | Skip reason | Meaning | Typical action |
|---|---|---|---|
| `BDTG001` | abstract component | Abstract component classes are skipped | Usually no action; track concrete subclasses instead |
| `BDTG002` | sealed component | Sealed components cannot be proxied through inheritance | Remove `sealed` only if tracking is important |
| `BDTG003` | generic component | Generic component proxies are not supported in the MVP | Leave untracked for now or use the simple mode on a wrapper |
| `BDTG004` | nested component | Nested component classes are skipped | Move the component to a top-level type if generator tracking is needed |
| `BDTG005` | already inherits `DevtoolsComponentBase` | Already covered by the simple inheritance mode | No action needed |
| `BDTG006` | already implements `IDevToolsComponentProxy` | Already a proxy/manual override | No action needed |
| `BDTG007` | non-`ComponentBase` shape | The code-behind class does not inherit `ComponentBase` | Check the component base chain or leave it out of generator mode |
| `BDTG008` | framework/internal component | Components under `Microsoft.AspNetCore` are intentionally skipped | No action needed |
| `BDTG009` | non-partial component | The MVP only targets partial component classes | Convert to a partial code-behind pattern if generator tracking is desired |

These diagnostics are designed to make a large-app trial practical: you can build the app, inspect the warning/info list, and quickly identify which component shapes are currently outside the supported proxy set.

## What The Generator Emits

For each tracked component, the generator should emit:

1. A proxy subtype that inherits from the original component
2. `IDevToolsComponentProxy` implementation returning the original component type
3. A `DevToolsTrackedComponentLifecycle` field or property
4. `SetParametersAsync` override that:
   - captures parameters with `DevToolsParameterSnapshotFactory`
   - calls `base.SetParametersAsync`
   - applies the snapshot through `DevToolsTrackedComponentLifecycle`
5. `OnAfterRender` override that forwards to the lifecycle
6. `Dispose` implementation that unregisters through the lifecycle
7. If nested proxy parent/child tracking is required, `BuildRenderTree` override that wraps the original render tree in `RenderWithParentScope`

The generator also emits a registration manifest so the activator registry can map original component types to proxy types without manual `AddBlazorDevToolsComponentProxy<TComponent, TProxy>()` calls.

## Current Limitations

- The generator currently only targets explicit partial component classes, which fits large `.razor.cs`-based apps but does not yet cover every possible component shape
- Sealed, generic, nested, abstract, and already-tracked components are intentionally skipped in this MVP
- DOM picker support remains opt-in and is not part of the proxy path yet
- The current MVP focuses on component tree tracking, parameter capture, and auto-refresh
- The runtime package now carries the generator analyzer automatically for package consumers of `BlazorDevTools.Runtime`
- The intended consumption model is through the packaged `BlazorDevTools.Runtime` NuGet. Plain project references inside this repo remain useful for development, but they do not fully model package-delivered analyzer behavior; the package-based validation fixture is the authoritative check for automatic analyzer delivery.
- More validation is still needed before recommending this path for a real large app, especially around unusual component patterns and long-term compatibility

# BlazorDevTools.Protocol

`BlazorDevTools.Protocol` contains shared contracts used by Blazor DevTools runtime and extension messaging.

Most consumers should not install this package directly.

- install `BlazorDevTools.Runtime` for normal app integration
- let `BlazorDevTools.Runtime` bring `BlazorDevTools.Protocol` transitively

This package is primarily published so runtime dependencies can resolve cleanly from GitHub Packages.

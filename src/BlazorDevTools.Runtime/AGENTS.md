# AGENTS.md

## Scope
- Applies to `src/BlazorDevTools.Runtime`.

## Focus
- Runtime service registration
- component tracking
- protocol payload production
- static web assets
- package-delivered analyzer wiring

## Guidance
- Preserve both integration modes:
  - simple inheritance-based mode
  - experimental generator/proxy mode
- Keep runtime changes lightweight and additive when possible.
- Validate with the narrowest relevant commands first:
  - `dotnet test tests/BlazorDevTools.Runtime.Tests/BlazorDevTools.Runtime.Tests.csproj`
  - `dotnet test tests/BlazorDevTools.CompatibilityFixture.Tests/BlazorDevTools.CompatibilityFixture.Tests.csproj` for generator/runtime integration changes
- For packaging work, also validate:
  - `dotnet pack src/BlazorDevTools.Runtime/BlazorDevTools.Runtime.csproj -c Release`
  - `dotnet build artifacts/package-consumer-validation/PackageConsumerValidation.csproj`

# AGENTS.md

## Scope
- Applies to `src/BlazorDevTools.Generators`.

## Focus
- source-generated proxy output
- eligibility rules
- generated manifest shape
- generator diagnostics

## Guidance
- Keep generator behavior explicit and conservative.
- Do not silently broaden eligibility without tests and docs updates.
- Preserve current runtime contract expectations for generated proxies.
- Validate with:
  - `dotnet test tests/BlazorDevTools.Generators.Tests/BlazorDevTools.Generators.Tests.csproj`
  - `dotnet test tests/BlazorDevTools.CompatibilityFixture.Tests/BlazorDevTools.CompatibilityFixture.Tests.csproj`

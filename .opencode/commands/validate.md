---
description: Run the narrowest useful validation for runtime, extension, generator, package, or full-repo work
agent: main-devtools-agent
---

/validate <scope>

Scopes:

- `runtime`
- `extension`
- `generator`
- `package`
- `full`

Instructions:

Follow `AGENTS.md` and run the narrowest useful validation for `$ARGUMENTS`.

Validation matrix:

- `runtime`
  - `dotnet build BlazorDevTools.sln`
  - `dotnet test tests/BlazorDevTools.Runtime.Tests/BlazorDevTools.Runtime.Tests.csproj`
- `extension`
  - `npm ci` in `src/BlazorDevTools.Extension`
  - `npm run build` in `src/BlazorDevTools.Extension`
- `generator`
  - `dotnet build BlazorDevTools.sln`
  - `dotnet test tests/BlazorDevTools.Generators.Tests/BlazorDevTools.Generators.Tests.csproj`
  - `dotnet test tests/BlazorDevTools.CompatibilityFixture.Tests/BlazorDevTools.CompatibilityFixture.Tests.csproj`
- `package`
  - `dotnet build BlazorDevTools.sln`
  - `dotnet test BlazorDevTools.sln`
  - `dotnet restore artifacts/package-consumer-validation/PackageConsumerValidation.csproj`
  - `dotnet build artifacts/package-consumer-validation/PackageConsumerValidation.csproj`
- `full`
  - `dotnet build BlazorDevTools.sln`
  - `dotnet test BlazorDevTools.sln`
  - `npm ci` in `src/BlazorDevTools.Extension`
  - `npm run build` in `src/BlazorDevTools.Extension`
  - `dotnet restore artifacts/package-consumer-validation/PackageConsumerValidation.csproj`
  - `dotnet build artifacts/package-consumer-validation/PackageConsumerValidation.csproj`

Return:

- what ran
- what passed or failed
- what was intentionally not run
- the smallest useful next step

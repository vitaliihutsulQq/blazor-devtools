---
description: Check or prepare Protocol and Runtime package/release work
agent: release-agent
---

/pack-release <mode>

Modes:

- `check`
- `prep`

Instructions:

Focus only on package/release work for `BlazorDevTools.Protocol` and `BlazorDevTools.Runtime`.

Always inspect:

- package versions
- NuGet metadata
- package readmes
- static web assets
- GitHub workflow expectations
- dependency order from Protocol to Runtime

Always run:

- `dotnet build BlazorDevTools.sln`
- `dotnet test BlazorDevTools.sln`
- `dotnet pack src/BlazorDevTools.Protocol/BlazorDevTools.Protocol.csproj -c Release`
- `dotnet pack src/BlazorDevTools.Runtime/BlazorDevTools.Runtime.csproj -c Release`

If `$ARGUMENTS` is `prep`, also:

- inspect the resulting package contents when relevant
- validate `artifacts/package-consumer-validation`
- review `.github/workflows/publish-packages.yml`

Return:

- versions detected
- package files produced
- whether runtime static web assets and analyzer delivery still look correct
- what remains to do before actual publishing

# AGENTS.md

Purpose
- Short guide for contributors and automation agents working on the BlazorDevTools repository.
- Project: Blazor WebAssembly developer tools with .NET projects (src/, tests/) and a TypeScript browser extension (src/BlazorDevTools.Extension).
- Two supported integration modes: a simple inheritance-based runtime integration and an experimental Roslyn generator / proxy path.

Scope
- Applies to the repository root. Subtree-level AGENTS.md (if present) overrides this file. Project files and CI are authoritative.

Repository layout (important projects)
- Root solution: BlazorDevTools.sln
- Key projects:
  - src/BlazorDevTools.Protocol — protocol contracts (package)
  - src/BlazorDevTools.Generators — Roslyn generator (experimental)
  - src/BlazorDevTools.Runtime — runtime Razor library + analyzer delivery
  - src/BlazorDevTools.SampleApp — local sample app
  - tests/BlazorDevTools.Runtime.Tests
  - tests/BlazorDevTools.Generators.Tests
  - tests/BlazorDevTools.CompatibilityFixture (large-app fixture)
  - tests/BlazorDevTools.ExternalConsumer (external-consumer-style validation app)
- Extension: src/BlazorDevTools.Extension
- Validation artifact: artifacts/package-consumer-validation (used to validate analyzer delivery from packages)
- Docs: README.md and docs/

Quick commands
- dotnet restore BlazorDevTools.sln
- dotnet build BlazorDevTools.sln
- dotnet test BlazorDevTools.sln
- Run specific tests:
  - dotnet test tests/BlazorDevTools.Runtime.Tests/BlazorDevTools.Runtime.Tests.csproj
  - dotnet test tests/BlazorDevTools.Generators.Tests/BlazorDevTools.Generators.Tests.csproj
  - dotnet test tests/BlazorDevTools.CompatibilityFixture.Tests/BlazorDevTools.CompatibilityFixture.Tests.csproj
- Pack packages (local validation):
  - dotnet pack src/BlazorDevTools.Protocol/BlazorDevTools.Protocol.csproj -c Release
  - dotnet pack src/BlazorDevTools.Runtime/BlazorDevTools.Runtime.csproj -c Release
- Sample and validation apps:
  - dotnet run --project src/BlazorDevTools.SampleApp/BlazorDevTools.SampleApp.csproj
  - dotnet run --project tests/BlazorDevTools.CompatibilityFixture/BlazorDevTools.CompatibilityFixture.csproj
  - dotnet run --project tests/BlazorDevTools.ExternalConsumer/BlazorDevTools.ExternalConsumer.csproj
  - dotnet publish tests/BlazorDevTools.ExternalConsumer/BlazorDevTools.ExternalConsumer.csproj -c Debug -o artifacts/external-consumer
- Package-consumer validation build:
  - dotnet restore artifacts/package-consumer-validation/PackageConsumerValidation.csproj && dotnet build artifacts/package-consumer-validation/PackageConsumerValidation.csproj
- Extension:
  - npm ci (in src/BlazorDevTools.Extension)
  - npm run build (in src/BlazorDevTools.Extension)

Guiding principles for changes
- Prefer the narrowest command that proves a change (unit/test/pack) rather than running the whole solution.
- Keep changes focused; avoid unrelated refactors in the same commit.
- Update docs and this file when you add new tooling, CI, or canonical commands.

Generator & validation notes (important)
- The generator/proxy integration is experimental. Validate generator changes by running:
  1) tests/BlazorDevTools.Generators.Tests
  2) tests/BlazorDevTools.CompatibilityFixture.Tests (compatibility fixture)
- Packaging flow: produce and validate packages in this order — Protocol first, then Runtime. Use dotnet pack and test package-consumer validation when relevant.
- For runtime analyzer delivery validation, use artifacts/package-consumer-validation and the external-consumer app as required.

Packaging and publishing (local guidance)
- Do not publish packages automatically from agent runs unless explicitly requested.
- Locally: dotnet pack the Protocol package, verify contents, then dotnet pack the Runtime package and validate analyzer delivery with the package consumer app.

Command execution guidance
- If a command fails because tooling is missing, report the missing tool rather than guessing.
- For extension-only changes, run npm ci and npm run build under src/BlazorDevTools.Extension and validate the produced artifacts.

Documentation expectations
- Keep README.md aligned with the install flow, supported integration modes, and experimental status of the generator path.
- Package READMEs should be short and installation-focused. docs/ may contain design notes and limitations.

Code style, tests, and quick practices
- Follow existing code and project conventions. Prefer readability and small focused changes.
- Add or update tests for behavior changes when appropriate. Prefer deterministic tests and keep tests near the behavior they validate.

Final checklist for changes
- In your PR or commit message, state what changed, what you validated, and what you could not validate locally (e.g., remote publishing).
- Mention any missing tests or tooling required to validate the change.

Maintenance
- Keep this file up to date when solution structure, packaging, or canonical commands change.

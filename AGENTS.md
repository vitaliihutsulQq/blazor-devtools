# AGENTS.md

## Purpose
- This repository contains a Blazor WebAssembly developer tools project inspired by Angular DevTools.
- The codebase mixes .NET projects under `src/` and `tests/` with a TypeScript browser extension under `src/BlazorDevTools.Extension`.
- The repository now includes both a simple inheritance-based runtime integration mode and an experimental generator/proxy integration mode.
- There are no Cursor rules in `.cursor/rules/` or `.cursorrules`.
- There are no Copilot rules in `.github/copilot-instructions.md`.

## Scope
- Applies to the repository root.
- A deeper `AGENTS.md` should take precedence for files in its subtree.
- Explicit project configuration beats this file.

## Repository Status
- Root solution: `BlazorDevTools.sln`.
- .NET projects:
  - `src/BlazorDevTools.Protocol` - protocol contracts class library and transitive package dependency.
  - `src/BlazorDevTools.Generators` - Roslyn source generator for the experimental proxy-based tracking path.
  - `src/BlazorDevTools.Runtime` - runtime Razor class library with static web assets and packaged analyzer delivery.
  - `src/BlazorDevTools.SampleApp` - local Blazor WebAssembly sample app.
  - `tests/BlazorDevTools.Runtime.Tests` - NUnit test project for runtime behavior.
  - `tests/BlazorDevTools.Generators.Tests` - NUnit test project for source generator behavior.
  - `tests/BlazorDevTools.CompatibilityFixture` - Blazor WebAssembly fixture simulating large code-behind-heavy consumer patterns.
  - `tests/BlazorDevTools.CompatibilityFixture.Tests` - NUnit test project verifying generator behavior against the compatibility fixture.
  - `tests/BlazorDevTools.ExternalConsumer` - external-consumer-style app validating the simple inheritance-based install flow.
- Extension project: `src/BlazorDevTools.Extension` - browser extension source and build scripts.
- Additional validation artifact: `artifacts/package-consumer-validation` - package-based app used to validate automatic analyzer delivery from the runtime package.
- Root documentation: `README.md`.
- Supporting docs live under `docs/`.
- NuGet restore is pinned to `nuget.org` through the repository `NuGet.config`.
- GitHub Actions workflows live under `.github/workflows/`.

## Rule Files Checked
- `.cursorrules`: not present.
- `.cursor/rules/`: not present.
- `.github/copilot-instructions.md`: not present.
- If any of these files are later added, read them before editing code and fold their rules into future updates of this file.

## Agent Workflow
- Inspect the repo for manifests and config files before making stack assumptions.
- Prefer commands defined by the repo over generic defaults.
- Keep changes focused and avoid unrelated refactors.
- When you add tooling, tests, workflows, docs, or conventions, update this file in the same change.
- Keep docs honest about experimental status and validation coverage.

## Command Discovery Order
- Check `README*`, docs, CI files, and local validation fixtures first.
- Then inspect: `*.sln`, `*.csproj`, `Directory.Build.props`, `global.json`.
- Then inspect: `package.json`, `package-lock.json`, `pnpm-lock.yaml`, `yarn.lock`.
- Use the first authoritative source you find instead of guessing.

## Build / Lint / Test Commands
### Current state
- Restore .NET dependencies: `dotnet restore BlazorDevTools.sln`
- Build .NET solution: `dotnet build BlazorDevTools.sln`
- Run .NET tests: `dotnet test BlazorDevTools.sln`
- Run runtime tests only: `dotnet test tests/BlazorDevTools.Runtime.Tests/BlazorDevTools.Runtime.Tests.csproj`
- Run generator tests only: `dotnet test tests/BlazorDevTools.Generators.Tests/BlazorDevTools.Generators.Tests.csproj`
- Run compatibility fixture tests only: `dotnet test tests/BlazorDevTools.CompatibilityFixture.Tests/BlazorDevTools.CompatibilityFixture.Tests.csproj`
- Pack protocol package: `dotnet pack src/BlazorDevTools.Protocol/BlazorDevTools.Protocol.csproj -c Release`
- Pack runtime package: `dotnet pack src/BlazorDevTools.Runtime/BlazorDevTools.Runtime.csproj -c Release`
- Run the sample app: `dotnet run --project src/BlazorDevTools.SampleApp/BlazorDevTools.SampleApp.csproj`
- Run the compatibility fixture: `dotnet run --project tests/BlazorDevTools.CompatibilityFixture/BlazorDevTools.CompatibilityFixture.csproj`
- Run the external consumer app: `dotnet run --project tests/BlazorDevTools.ExternalConsumer/BlazorDevTools.ExternalConsumer.csproj`
- Publish the external consumer app for install validation: `dotnet publish tests/BlazorDevTools.ExternalConsumer/BlazorDevTools.ExternalConsumer.csproj -c Debug -o artifacts/external-consumer`
- Restore/build the package validation app: `dotnet restore artifacts/package-consumer-validation/PackageConsumerValidation.csproj && dotnet build artifacts/package-consumer-validation/PackageConsumerValidation.csproj`
- Install extension dependencies: `npm ci` in `src/BlazorDevTools.Extension`
- Build the extension: `npm run build` in `src/BlazorDevTools.Extension`
- Lint: none configured yet for either .NET or TypeScript.

## Command Execution Guidance
- Prefer the narrowest command that proves the change works.
- For generator work, run the generator tests and compatibility fixture tests first.
- For packaging changes, run local pack commands and inspect package contents when relevant.
- For extension-only changes, prefer `npm ci` and `npm run build` in `src/BlazorDevTools.Extension`.
- If a command fails because tooling is missing, report the missing tool instead of guessing.

## Documentation Expectations
- Keep `README.md` aligned with the current install flow, integration modes, workflows, and package versions.
- Keep package readmes concise and installation-focused.
- Keep `docs/` honest about experimental behavior, validation scope, and limitations.
- Do not describe zero-config large-app support as production-ready unless the repository actually validates it end to end.

## Code Style Baseline
- Follow existing code first; if none exists, use the conventions below.
- Optimize for readability, maintainability, and low surprise.
- Keep files focused; do not mix unrelated concerns in one change.
- Prefer conventional code over clever abstractions.

## Imports and Dependencies
- Group imports consistently: standard library/framework, third-party, local.
- Remove unused imports.
- Prefer explicit imports over wildcard imports unless the ecosystem strongly favors them.
- Avoid adding new dependencies when the standard library or current stack already solves the problem.

## Formatting
- Use the repository formatter when one exists.
- If no formatter exists, preserve surrounding style within the file.
- Keep indentation and line wrapping consistent.
- Avoid whitespace-only churn unless required by formatting.

## Types and Interfaces
- Prefer explicit public API types.
- Keep internal types simple and intention-revealing.
- Avoid `any`-style escape hatches unless there is a clear, localized reason.
- Model nullability and optional values deliberately.
- Validate untrusted input at boundaries.

## Naming
- Use descriptive names that reveal intent.
- Prefer full words over unclear abbreviations.
- Match the conventions of the language and framework in use.
- Name booleans as predicates, such as `isReady`, `hasErrors`, or `canSave`.
- Name async operations for their effect, not their implementation detail.

## Functions and Methods
- Give each function one clear responsibility.
- Prefer small functions with obvious control flow.
- Use guard clauses for invalid states and early exits.
- Pass explicit inputs instead of reaching into shared state when practical.
- Avoid hidden side effects.

## Error Handling
- Fail fast on invalid input and impossible states.
- Do not swallow errors silently unless the behavior is intentionally defensive and documented.
- Add context when rethrowing or propagating failures.
- Return or throw the most specific error shape the stack supports.
- User-facing messages should be actionable; logs should contain diagnostic detail.

## Logging and Diagnostics
- Log meaningful events, not routine noise.
- Include enough context to debug issues without leaking secrets.
- Never log passwords, tokens, connection strings, or personal data.
- Prefer structured logging when the stack supports it.
- Generator diagnostics should use stable IDs and actionable wording.

## Tests
- Add or update tests for behavior changes when a test framework exists.
- Keep tests close to the behavior they validate.
- Favor deterministic tests over timing-sensitive tests.
- Mock only real external boundaries.
- For bug fixes, add a regression test when feasible.
- Prefer NUnit for .NET tests unless an existing project already uses something else.

## Configuration and Secrets
- Never hardcode secrets.
- Use environment variables, user-level NuGet sources, or the project's documented secret-management flow.
- Document new config keys when project docs exist.
- Prefer safe local defaults when practical.

## Change Management
- Make the smallest change that fully solves the problem.
- Avoid opportunistic refactors unless required for correctness or maintainability.
- Preserve user changes you did not author.
- If you add tooling or commands, update this file with the exact canonical usage.

## Final Check Expectations
- State what changed.
- State what you validated.
- State what you could not validate.
- Mention missing tests or tooling explicitly.

## Maintenance Note
- Update this file when solution structure, commands, workflows, packaging, or docs expectations change.

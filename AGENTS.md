# AGENTS.md
## Purpose
- This repository contains the initial scaffold for a Blazor WebAssembly developer tools project inspired by Angular DevTools.
- The codebase mixes .NET projects under `src/` and `tests/` with a minimal TypeScript browser extension scaffold under `src/BlazorDevTools.Extension`.
- There are no Cursor rules in `.cursor/rules/` or `.cursorrules`.
- There are no Copilot rules in `.github/copilot-instructions.md`.
- Keep changes minimal and scaffold-friendly until real runtime, protocol, and extension features are implemented.
## Scope
- Applies to the repository root.
- A deeper `AGENTS.md` should take precedence for files in its subtree.
- Explicit project configuration beats this file.
## Repository Status
- Root solution: `BlazorDevTools.sln`.
- .NET projects:
  - `src/BlazorDevTools.Protocol` - protocol contracts class library.
  - `src/BlazorDevTools.Runtime` - runtime integration Razor class library with static web assets.
  - `src/BlazorDevTools.SampleApp` - Blazor WebAssembly host used for local development.
  - `tests/BlazorDevTools.Runtime.Tests` - NUnit test project for runtime behavior.
  - `tests/BlazorDevTools.ExternalConsumer` - separate Blazor WebAssembly app that validates external-consumer installation.
- Extension project: `src/BlazorDevTools.Extension` - minimal TypeScript browser extension scaffold with `package.json` and `tsconfig.json`.
- Root documentation: `README.md` - installation and verification guide for the extension and runtime package.
- NuGet restore is pinned to `nuget.org` through the repository `NuGet.config` to avoid machine-specific package sources leaking into builds.
- No dedicated linter or CI workflow is configured yet.
## Rule Files Checked
- `.cursorrules`: not present.
- `.cursor/rules/`: not present.
- `.github/copilot-instructions.md`: not present.
- If any of these files are later added, read them before editing code and fold their rules into future updates of this file.
## Agent Workflow
- Inspect the repo for manifests and config files before making stack assumptions.
- Prefer commands defined by the repo over generic defaults.
- Keep changes focused and avoid unrelated refactors.
- When you add tooling, tests, or conventions, update this file in the same change.
- Report assumptions clearly when the repo does not provide enough context.
## Command Discovery Order
- Check `README*`, CI files, and local task scripts first.
- Then inspect: `*.sln`, `*.csproj`, `Directory.Build.props`, `global.json`.
- Then inspect: `package.json`, `pnpm-lock.yaml`, `yarn.lock`, `package-lock.json`.
- Then inspect: `pyproject.toml`, `requirements*.txt`, `tox.ini`.
- Then inspect: `Cargo.toml`, `go.mod`, `pom.xml`, `build.gradle*`.
- Use the first authoritative source you find instead of guessing.
## Build / Lint / Test Commands
### Current state
- Restore .NET dependencies: `dotnet restore BlazorDevTools.sln`
- Build .NET solution: `dotnet build BlazorDevTools.sln`
- Run .NET tests: `dotnet test BlazorDevTools.sln`
- Run runtime tests only: `dotnet test tests/BlazorDevTools.Runtime.Tests/BlazorDevTools.Runtime.Tests.csproj`
- Run a single NUnit test: `dotnet test tests/BlazorDevTools.Runtime.Tests/BlazorDevTools.Runtime.Tests.csproj --filter "FullyQualifiedName~BlazorDevTools.Runtime.Tests.RuntimeRegistrationTests"`
- Pack the runtime NuGet package: `dotnet pack src/BlazorDevTools.Runtime/BlazorDevTools.Runtime.csproj -c Release`
- Run the sample app: `dotnet run --project src/BlazorDevTools.SampleApp/BlazorDevTools.SampleApp.csproj`
- Run the external consumer app: `dotnet run --project tests/BlazorDevTools.ExternalConsumer/BlazorDevTools.ExternalConsumer.csproj`
- Publish the external consumer app for install validation: `dotnet publish tests/BlazorDevTools.ExternalConsumer/BlazorDevTools.ExternalConsumer.csproj -c Debug -o artifacts/external-consumer`
- Install extension dependencies: `npm install` in `src/BlazorDevTools.Extension`
- Build the extension: `npm run build` in `src/BlazorDevTools.Extension`
- Lint: none configured yet for either .NET or TypeScript.
## Command Execution Guidance
- Prefer the narrowest command that proves your change works.
- For a bug fix, run the relevant single test first, then the nearest broader suite.
- Avoid expensive repo-wide validation when a smaller command is enough.
- If a command fails because tooling is missing, report the missing tool instead of guessing.
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
- Do not swallow errors silently.
- Add context when rethrowing or propagating failures.
- Return or throw the most specific error shape the stack supports.
- User-facing messages should be actionable; logs should contain diagnostic detail.
## Logging and Diagnostics
- Log meaningful events, not routine noise.
- Include enough context to debug issues without leaking secrets.
- Never log passwords, tokens, connection strings, or personal data.
- Prefer structured logging when the stack supports it.
## Tests
- Add or update tests for behavior changes when a test framework exists.
- Keep tests close to the behavior they validate.
- Favor deterministic tests over timing-sensitive tests.
- Mock only real external boundaries.
- For bug fixes, add a regression test when feasible.
- Prefer NUnit for .NET tests unless an existing project already uses something else.
## Configuration and Secrets
- Never hardcode secrets.
- Use environment variables or the project's secret-management mechanism.
- Document new config keys when project docs exist.
- Prefer safe local defaults when practical.
## Change Management
- Make the smallest change that fully solves the problem.
- Avoid opportunistic refactors unless they are required for correctness or maintainability.
- Preserve user changes you did not author.
- If you add tooling or commands, update this file with the exact canonical usage.
## Final Check Expectations
- State what changed.
- State what you validated.
- State what you could not validate.
- Mention missing tests or tooling explicitly.
## Maintenance Note
- Update this file when solution structure, commands, or extension tooling changes.
- Add canonical linting, formatting, and packaging commands here as soon as they exist.

# Publishing And Release Flow

## CI Workflow

File: `.github/workflows/ci.yml`

Purpose:

- restore, build, and test the .NET solution
- install and build the browser extension
- validate changes on pull requests and pushes to `main`

It does not publish packages.

## Publish Workflow

File: `.github/workflows/publish-packages.yml`

Triggers:

- `workflow_dispatch`
- version tag pushes such as `v0.1.3`

Permissions:

- `contents: read`
- `packages: write`

Authentication:

- uses `GITHUB_TOKEN`
- publishes to `https://nuget.pkg.github.com/vitaliihutsulQq/index.json`

## Publishing Order

Publishing order matters because `BlazorDevTools.Runtime` depends on `BlazorDevTools.Protocol`.

The workflow publishes in this order:

1. `BlazorDevTools.Protocol`
2. `BlazorDevTools.Runtime`

Both push steps use `--skip-duplicate` so rerunning the workflow for an already-published version is non-destructive.

## Local Pack Commands

```bash
dotnet pack src/BlazorDevTools.Protocol/BlazorDevTools.Protocol.csproj -c Release
dotnet pack src/BlazorDevTools.Runtime/BlazorDevTools.Runtime.csproj -c Release
```

## Version Bump Flow

Current versioning is still csproj-driven.

When releasing a new version:

1. update package versions in:
   - `src/BlazorDevTools.Protocol/BlazorDevTools.Protocol.csproj`
   - `src/BlazorDevTools.Runtime/BlazorDevTools.Runtime.csproj`
2. build and test locally
3. pack locally if needed for validation
4. push the change to `main`
5. either:
   - create and push a version tag like `v0.1.3`, or
   - run the publish workflow manually

## Updating A Consuming App

After publishing a new version:

```bash
dotnet add package BlazorDevTools.Runtime --version 0.1.3
```

Then restore and rebuild the app.

If the extension also changed, rebuild and reload the unpacked extension too.

## Repository Settings To Check

Before relying on automated publishing:

- make sure GitHub Actions has package write permission
- make sure the repository is allowed to publish packages with `GITHUB_TOKEN`
- make sure package visibility/access settings match the intended consumers

## Notes On CI Authentication

The CI workflow currently authenticates to GitHub Packages for restore using `GH_PACKAGES_TOKEN`.

That is separate from the publish workflow, which uses `GITHUB_TOKEN` for publishing.

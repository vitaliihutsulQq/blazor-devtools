# Compatibility Fixture

`tests/BlazorDevTools.CompatibilityFixture` is an in-repo validation app that simulates common component patterns from large Blazor WebAssembly apps with heavy `.razor.cs` usage.

## Representative Shapes Included

Tracked by the generator MVP:

- `Pages/CaseDetails.razor` + `Pages/CaseDetails.razor.cs`
  - routed page
  - explicit `: ComponentBase`
  - `[Parameter]` route value
- `Components/CaseWorkspace.razor` + `Components/CaseWorkspace.razor.cs`
  - explicit `: ComponentBase`
  - `[Inject]` properties
  - `[Parameter]` properties
  - async `OnParametersSetAsync`
  - nested child components
- `Components/DocumentList.razor` + `.razor.cs`
  - explicit `: ComponentBase`
  - nested hierarchy
- `Components/DocumentCard.razor` + `.razor.cs`
  - simple parameterized leaf component
- `Components/ActivityTimeline.razor` + `.razor.cs`
  - explicit `: ComponentBase`
  - async lifecycle work
- `Components/ActivityTimelineItem.razor` + `.razor.cs`
  - simple parameterized leaf component

Intentionally skipped by the generator MVP:

- `Components/SkippedSealedPanel.razor` + `.razor.cs`
  - skipped because the component class is `sealed`
- `Components/SkippedGenericList.razor` + `.razor.cs`
  - skipped because the component is generic
- `Components/AlreadyTrackedWidget.razor`
  - skipped because it already inherits `DevtoolsComponentBase`
  - still tracked through the existing inheritance-based mode

These skip reasons now surface through generator diagnostics during a real app build, which makes it easier to assess rollout risk before trying the package in a large codebase.

## How Validation Works

- `GeneratedProxySmokeCheck.cs` in the fixture app references the generated proxy and manifest types directly, so the build fails if expected generator output is missing
- `tests/BlazorDevTools.CompatibilityFixture.Tests` verifies:
  - eligible components get generated proxies
  - intentionally skipped shapes do not get proxies
  - the generated manifest contains the expected registrations
  - the simple inheritance-based mode still coexists

## Confidence Level

This fixture gives moderate confidence for trying the package in a real large app when the app has many partial `.razor.cs : ComponentBase` components.

It does not yet prove support for every enterprise component pattern. It is still important to validate against:

- custom intermediate base classes
- nested or non-public component types
- generic components
- sealed components
- any app-specific patterns that differ from straightforward `ComponentBase` partial classes

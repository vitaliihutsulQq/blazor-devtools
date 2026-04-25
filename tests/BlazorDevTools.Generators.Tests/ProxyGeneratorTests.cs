using BlazorDevTools.Runtime;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;

namespace BlazorDevTools.Generators.Tests;

public class ProxyGeneratorTests
{
    [Test]
    public void Generator_emits_proxy_and_manifest_for_eligible_components_only()
    {
        const string source = """
            using System;
            using Microsoft.AspNetCore.Components;
            using BlazorDevTools.Runtime;

            namespace TestApp;

            public partial class EligibleComponent : ComponentBase { }
            public abstract partial class AbstractComponent : ComponentBase { }
            public sealed partial class SealedComponent : ComponentBase { }
            public partial class GenericComponent<T> : ComponentBase { }
            public partial class AlreadyTrackedComponent : DevtoolsComponentBase { }
            public partial class PlainClass { }
            public class NonPartialComponent : ComponentBase { }
            """;

        var result = RunGenerator(source);
        var manifest = result.GeneratedSources.Single(entry => entry.Key.StartsWith("BDTManifest_", StringComparison.Ordinal)).Value;

        Assert.That(result.GeneratedSources.Keys, Does.Contain("EligibleComponent__BlazorDevToolsProxy.g.cs"));
        Assert.That(manifest, Does.Contain("EligibleComponent__BlazorDevToolsProxy"));
        Assert.That(manifest, Does.Not.Contain("AbstractComponent__BlazorDevToolsProxy"));
        Assert.That(manifest, Does.Not.Contain("SealedComponent__BlazorDevToolsProxy"));
        Assert.That(manifest, Does.Not.Contain("GenericComponent__BlazorDevToolsProxy"));
        Assert.That(manifest, Does.Not.Contain("AlreadyTrackedComponent__BlazorDevToolsProxy"));
    }

    [Test]
    public void Generator_emits_registration_manifest_for_multiple_components()
    {
        const string source = """
        using Microsoft.AspNetCore.Components;

        namespace TestApp;

        public partial class FirstComponent : ComponentBase { }
        public partial class SecondComponent : ComponentBase { }
        """;

        var result = RunGenerator(source);
        var manifest = result.GeneratedSources.Single(entry => entry.Key.StartsWith("BDTManifest_", StringComparison.Ordinal)).Value;

        Assert.That(manifest, Does.Contain("new(typeof(global::TestApp.FirstComponent), typeof(global::TestApp.FirstComponent__BlazorDevToolsProxy))"));
        Assert.That(manifest, Does.Contain("new(typeof(global::TestApp.SecondComponent), typeof(global::TestApp.SecondComponent__BlazorDevToolsProxy))"));
    }

    [Test]
    public void Generator_uses_exact_symbol_namespace_for_proxy_references()
    {
        const string source = """
        using Microsoft.AspNetCore.Components;

        namespace A.B.C.Widget
        {
            public partial class Widget : ComponentBase { }
        }

        namespace A.B.C.Pages.TimeEntry
        {
            public partial class TimeEntry : ComponentBase { }
        }
        """;

        var result = RunGenerator(source);
        var widgetProxy = result.GeneratedSources["Widget__BlazorDevToolsProxy.g.cs"];
        var timeEntryProxy = result.GeneratedSources["TimeEntry__BlazorDevToolsProxy.g.cs"];
        var manifest = result.GeneratedSources.Single(entry => entry.Key.StartsWith("BDTManifest_", StringComparison.Ordinal)).Value;

        Assert.That(widgetProxy, Does.Contain("namespace A.B.C.Widget;"));
        Assert.That(timeEntryProxy, Does.Contain("namespace A.B.C.Pages.TimeEntry;"));

        Assert.That(manifest, Does.Contain("new(typeof(global::A.B.C.Widget.Widget), typeof(global::A.B.C.Widget.Widget__BlazorDevToolsProxy))"));
        Assert.That(manifest, Does.Contain("new(typeof(global::A.B.C.Pages.TimeEntry.TimeEntry), typeof(global::A.B.C.Pages.TimeEntry.TimeEntry__BlazorDevToolsProxy))"));

        Assert.That(manifest, Does.Not.Contain("global::A.B.C.Widget__BlazorDevToolsProxy"));
        Assert.That(manifest, Does.Not.Contain("global::A.B.C.Pages.TimeEntry__BlazorDevToolsProxy"));
    }

    [Test]
    public void Generator_emits_automatic_dom_anchor_for_proxy_inspect_mode()
    {
        const string source = """
        using Microsoft.AspNetCore.Components;

        namespace TestApp;

        public partial class InspectableComponent : ComponentBase { }
        """;

        var result = RunGenerator(source);
        var proxy = result.GeneratedSources["InspectableComponent__BlazorDevToolsProxy.g.cs"];

        Assert.That(proxy, Does.Contain("private string DomMarkerId => TrackingLifecycle.ComponentId;"));
        Assert.That(proxy, Does.Contain("TrackingLifecycle.ApplySnapshot(DevToolsOriginalComponentType, ParentComponentId, snapshots, injectedServices, cascadingParameters, DomMarkerId);"));
        Assert.That(proxy, Does.Contain("TrackingLifecycle.RenderWithParentScopeAndDomMarker(builder, base.BuildRenderTree);"));
    }

    [Test]
    public void Generator_emits_actionable_skip_diagnostics_for_fixture_like_shapes()
    {
        const string source = """
        using System;
        using Microsoft.AspNetCore.Components;
        using BlazorDevTools.Runtime;

        namespace TestApp.Components
        {
            public sealed partial class SkippedSealedPanel : ComponentBase { }
            public partial class SkippedGenericList<TItem> : ComponentBase { }
            public abstract partial class AbstractWorkspace : ComponentBase { }
            public partial class AlreadyTrackedWidget : DevtoolsComponentBase { }
            public partial class ExistingProxyWidget : ComponentBase, IDevToolsComponentProxy
            {
                public Type DevToolsOriginalComponentType => typeof(ExistingProxyWidget);
                public void Dispose() { }
            }

            public class NonPartialPage : ComponentBase { }
            public partial class NonComponentCodeBehind : IDisposable { public void Dispose() { } }

            public partial class Outer
            {
                public partial class NestedPanel : ComponentBase { }
            }
        }

        namespace Microsoft.AspNetCore.TestHost
        {
            public partial class FrameworkPanel : ComponentBase { }
        }
        """;

        var result = RunGenerator(source, filePath: "TestComponents.razor.cs");

        AssertDiagnostic(result.Diagnostics, "BDTG001", "AbstractWorkspace");
        AssertDiagnostic(result.Diagnostics, "BDTG002", "SkippedSealedPanel");
        AssertDiagnostic(result.Diagnostics, "BDTG003", "SkippedGenericList");
        AssertDiagnostic(result.Diagnostics, "BDTG004", "NestedPanel");
        AssertDiagnostic(result.Diagnostics, "BDTG005", "AlreadyTrackedWidget");
        AssertDiagnostic(result.Diagnostics, "BDTG006", "ExistingProxyWidget");
        AssertDiagnostic(result.Diagnostics, "BDTG008", "FrameworkPanel");
        AssertDiagnostic(result.Diagnostics, "BDTG009", "NonPartialPage");
    }

    [Test]
    public void Generator_emits_non_componentbase_diagnostic_for_razor_codebehind_shape()
    {
        const string source = """
        using System;
        namespace TestApp.Components;

        public partial class NonComponentCodeBehind : IDisposable
        {
            public void Dispose() { }
        }
        """;

        var result = RunGenerator(source, filePath: "NonComponentCodeBehind.razor.cs");

        AssertDiagnostic(result.Diagnostics, "BDTG007", "NonComponentCodeBehind");
    }

    [Test]
    public void Generator_treats_partial_razor_codebehind_with_blazor_component_signals_as_component_even_without_explicit_componentbase()
    {
        const string source = """
        using Microsoft.AspNetCore.Components;

        namespace TestApp.Pages;

        public partial class ChristopherEdwardNolan
        {
            [Inject]
            public NavigationManager Navigation { get; set; } = default!;

            protected override Task OnInitializedAsync()
            {
                StateHasChanged();
                return Task.CompletedTask;
            }
        }
        """;

        var result = RunGenerator(source, filePath: "ChristopherEdwardNolan.razor.cs");

        Assert.That(result.GeneratedSources.Keys, Does.Contain("ChristopherEdwardNolan__BlazorDevToolsProxy.g.cs"));
        Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Id == "BDTG007"), Is.False,
            string.Join(Environment.NewLine, result.Diagnostics.Select(diagnostic => $"{diagnostic.Id}: {diagnostic.GetMessage()}")));
    }

    private static GeneratorExecutionResult RunGenerator(string source, string filePath = "TestInput.cs")
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: filePath);
        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTests",
            syntaxTrees: [syntaxTree],
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        ISourceGenerator generator = new BlazorDevToolsProxyGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        return new GeneratorExecutionResult(
            runResult.Results
            .SelectMany(result => result.GeneratedSources)
            .ToDictionary(sourceResult => sourceResult.HintName, sourceResult => sourceResult.SourceText.ToString()),
            runResult.Results.SelectMany(result => result.Diagnostics).ToArray());
    }

    private static void AssertDiagnostic(IEnumerable<Diagnostic> diagnostics, string diagnosticId, string componentName)
    {
        Assert.That(diagnostics.Any(diagnostic => diagnostic.Id == diagnosticId && diagnostic.GetMessage().Contains(componentName, StringComparison.Ordinal)), Is.True,
            $"Expected diagnostic {diagnosticId} for {componentName}. Actual: {string.Join(Environment.NewLine, diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"))}");
    }

    private static ImmutableArray<MetadataReference> GetMetadataReferences()
    {
        var trustedPlatformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            ?? [];

        var explicitAssemblies = new[]
        {
            typeof(ComponentBase).Assembly.Location,
            typeof(DevtoolsComponentBase).Assembly.Location,
            typeof(object).Assembly.Location
        };

        return trustedPlatformAssemblies
            .Concat(explicitAssemblies)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Distinct(MetadataReferencePathComparer.Instance)
            .ToImmutableArray();
    }

    private sealed class MetadataReferencePathComparer : IEqualityComparer<MetadataReference>
    {
        public static MetadataReferencePathComparer Instance { get; } = new();

        public bool Equals(MetadataReference? x, MetadataReference? y)
        {
            return StringComparer.OrdinalIgnoreCase.Equals((x as PortableExecutableReference)?.FilePath, (y as PortableExecutableReference)?.FilePath);
        }

        public int GetHashCode(MetadataReference obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode((obj as PortableExecutableReference)?.FilePath ?? string.Empty);
        }
    }

    private sealed record GeneratorExecutionResult(IReadOnlyDictionary<string, string> GeneratedSources, IReadOnlyList<Diagnostic> Diagnostics);
}

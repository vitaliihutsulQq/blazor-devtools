using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BlazorDevTools.Generators;

[Generator]
public sealed class BlazorDevToolsProxyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var assemblyName = context.CompilationProvider.Select(static (compilation, _) => SanitizeIdentifier(compilation.AssemblyName ?? "BlazorDevToolsGenerated"));

        var componentAnalyses = context.SyntaxProvider
            .CreateSyntaxProvider(static (node, _) => node is ClassDeclarationSyntax, static (generatorContext, _) => AnalyzeComponent(generatorContext))
            .Where(static analysis => analysis is not null)
            .Select(static (analysis, _) => analysis!)
            .Collect()
            .Select(static (analyses, _) => analyses.Distinct(ComponentAnalysisEqualityComparer.Instance).ToImmutableArray());

        context.RegisterSourceOutput(componentAnalyses.Combine(assemblyName), static (productionContext, source) =>
        {
            var (analyses, manifestSuffix) = source;
            var candidates = analyses.Where(static analysis => analysis.Candidate is not null).Select(static analysis => analysis.Candidate!).ToImmutableArray();

            foreach (var analysis in analyses.Where(static analysis => analysis.Diagnostic is not null))
            {
                productionContext.ReportDiagnostic(analysis.Diagnostic!);
            }

            foreach (var candidate in candidates)
            {
                productionContext.AddSource(
                    $"{candidate.ProxyTypeName}.g.cs",
                    SourceText.From(RenderProxy(candidate), Encoding.UTF8));
            }

            productionContext.AddSource(
                $"BDTManifest_{manifestSuffix}.g.cs",
                SourceText.From(RenderManifest(candidates, manifestSuffix), Encoding.UTF8));
        });
    }

    private static ComponentAnalysis? AnalyzeComponent(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var fullyQualifiedTypeName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var containingNamespace = symbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : symbol.ContainingNamespace.ToDisplayString();

        var skipResult = GetSkipResult(symbol, classDeclaration);
        if (skipResult is not null)
        {
            return new ComponentAnalysis(symbol, null, CreateDiagnostic(skipResult.Value.Descriptor, classDeclaration.Identifier.GetLocation(), symbol.Name));
        }

        if (!IsEligibleForGeneration(symbol, classDeclaration))
        {
            return null;
        }

        return new ComponentAnalysis(
            symbol,
            new ComponentCandidate(
                symbol,
                containingNamespace,
                symbol.Name,
                $"{symbol.Name}__BlazorDevToolsProxy",
                fullyQualifiedTypeName,
                GetFullyQualifiedProxyTypeName(containingNamespace, $"{symbol.Name}__BlazorDevToolsProxy")),
            null);
    }

    private static string GetFullyQualifiedProxyTypeName(string? containingNamespace, string proxyTypeName)
    {
        return string.IsNullOrWhiteSpace(containingNamespace)
            ? $"global::{proxyTypeName}"
            : $"global::{containingNamespace}.{proxyTypeName}";
    }

    private static bool IsEligibleForGeneration(INamedTypeSymbol symbol, ClassDeclarationSyntax classDeclaration)
    {
        var behavesLikeImplicitRazorComponent = IsImplicitRazorComponentCodeBehind(classDeclaration);

        if (symbol.TypeKind != TypeKind.Class ||
            symbol.IsAbstract ||
            symbol.IsSealed ||
            symbol.IsGenericType ||
            symbol.ContainingType is not null ||
            symbol.DeclaredAccessibility == Accessibility.Private ||
            symbol.Name.EndsWith("__BlazorDevToolsProxy", StringComparison.Ordinal) ||
            !classDeclaration.Modifiers.Any(static modifier => modifier.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)) ||
            InheritsFrom(symbol, "BlazorDevTools.Runtime.DevtoolsComponentBase") ||
            Implements(symbol, "BlazorDevTools.Runtime.IDevToolsComponentProxy") ||
            !(InheritsFrom(symbol, "Microsoft.AspNetCore.Components.ComponentBase") || behavesLikeImplicitRazorComponent))
        {
            return false;
        }

        var containingNamespace = symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        if (containingNamespace.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) ||
            containingNamespace.StartsWith("BlazorDevTools.Runtime", StringComparison.Ordinal) ||
            containingNamespace.StartsWith("BlazorDevTools.Protocol", StringComparison.Ordinal) ||
            containingNamespace.StartsWith("BlazorDevTools.Generators", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    private static SkipResult? GetSkipResult(INamedTypeSymbol symbol, ClassDeclarationSyntax classDeclaration)
    {
        if (symbol.TypeKind != TypeKind.Class || symbol.DeclaredAccessibility == Accessibility.Private)
        {
            return null;
        }

        var isRazorCodeBehind = contextLooksLikeRazorCodeBehind(classDeclaration.SyntaxTree.FilePath);
        var behavesLikeImplicitRazorComponent = IsImplicitRazorComponentCodeBehind(classDeclaration);
        var derivesDevtoolsBase = InheritsFrom(symbol, "BlazorDevTools.Runtime.DevtoolsComponentBase");
        var implementsProxy = Implements(symbol, "BlazorDevTools.Runtime.IDevToolsComponentProxy");
        var derivesComponentBase = InheritsFrom(symbol, "Microsoft.AspNetCore.Components.ComponentBase");
        var containingNamespace = symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        var looksLikeTrackedComponent = derivesComponentBase || derivesDevtoolsBase || implementsProxy || isRazorCodeBehind || behavesLikeImplicitRazorComponent;

        if (!looksLikeTrackedComponent)
        {
            return null;
        }

        if (containingNamespace.StartsWith("BlazorDevTools.Runtime", StringComparison.Ordinal) ||
            containingNamespace.StartsWith("BlazorDevTools.Protocol", StringComparison.Ordinal) ||
            containingNamespace.StartsWith("BlazorDevTools.Generators", StringComparison.Ordinal))
        {
            return null;
        }

        if (symbol.Name.EndsWith("__BlazorDevToolsProxy", StringComparison.Ordinal))
        {
            return null;
        }

        if (symbol.IsAbstract)
        {
            return new SkipResult(BlazorDevToolsGeneratorDiagnostics.AbstractComponentSkipped);
        }

        if (symbol.IsSealed)
        {
            return new SkipResult(BlazorDevToolsGeneratorDiagnostics.SealedComponentSkipped);
        }

        if (symbol.IsGenericType)
        {
            return new SkipResult(BlazorDevToolsGeneratorDiagnostics.GenericComponentSkipped);
        }

        if (symbol.ContainingType is not null)
        {
            return new SkipResult(BlazorDevToolsGeneratorDiagnostics.NestedComponentSkipped);
        }

        if (derivesDevtoolsBase)
        {
            return new SkipResult(BlazorDevToolsGeneratorDiagnostics.AlreadyTrackedComponentSkipped);
        }

        if (implementsProxy)
        {
            return new SkipResult(BlazorDevToolsGeneratorDiagnostics.ExistingProxySkipped);
        }

        if (!derivesComponentBase && !behavesLikeImplicitRazorComponent)
        {
            if (classDeclaration.Modifiers.Any(static modifier => modifier.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)) || isRazorCodeBehind)
            {
                return new SkipResult(BlazorDevToolsGeneratorDiagnostics.NonComponentBaseSkipped);
            }

            return null;
        }

        if (!classDeclaration.Modifiers.Any(static modifier => modifier.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)))
        {
            return new SkipResult(BlazorDevToolsGeneratorDiagnostics.NonPartialComponentSkipped);
        }

        if (containingNamespace.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal))
        {
            return new SkipResult(BlazorDevToolsGeneratorDiagnostics.FrameworkComponentSkipped);
        }

        return null;
    }

    private static bool contextLooksLikeRazorCodeBehind(string? filePath)
    {
        var path = filePath ?? string.Empty;
        if (path.Length == 0)
        {
            return false;
        }

        return path.EndsWith(".razor.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImplicitRazorComponentCodeBehind(ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration.Modifiers.Any(static modifier => modifier.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword))
               && contextLooksLikeRazorCodeBehind(classDeclaration.SyntaxTree.FilePath)
               && LooksLikeBlazorComponentCodeBehind(classDeclaration);
    }

    private static bool LooksLikeBlazorComponentCodeBehind(ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration.Members.Any(static member =>
            member switch
            {
                PropertyDeclarationSyntax property => HasKnownBlazorAttribute(property.AttributeLists),
                MethodDeclarationSyntax method => LooksLikeLifecycleOverride(method) || ContainsStateHasChangedInvocation(method),
                _ => false
            });
    }

    private static bool HasKnownBlazorAttribute(SyntaxList<AttributeListSyntax> attributeLists)
    {
        foreach (var attributeList in attributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (name.EndsWith("Inject", StringComparison.Ordinal) ||
                    name.EndsWith("InjectAttribute", StringComparison.Ordinal) ||
                    name.EndsWith("Parameter", StringComparison.Ordinal) ||
                    name.EndsWith("ParameterAttribute", StringComparison.Ordinal) ||
                    name.EndsWith("CascadingParameter", StringComparison.Ordinal) ||
                    name.EndsWith("CascadingParameterAttribute", StringComparison.Ordinal) ||
                    name.EndsWith("SupplyParameterFromQuery", StringComparison.Ordinal) ||
                    name.EndsWith("SupplyParameterFromQueryAttribute", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool LooksLikeLifecycleOverride(MethodDeclarationSyntax method)
    {
        if (!method.Modifiers.Any(static modifier => modifier.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.OverrideKeyword)))
        {
            return false;
        }

        return method.Identifier.ValueText is "OnInitialized"
            or "OnInitializedAsync"
            or "OnParametersSet"
            or "OnParametersSetAsync"
            or "OnAfterRender"
            or "OnAfterRenderAsync"
            or "ShouldRender"
            or "SetParametersAsync"
            or "BuildRenderTree";
    }

    private static bool ContainsStateHasChangedInvocation(MethodDeclarationSyntax method)
    {
        return method.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(static invocation =>
            invocation.Expression switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText == "StateHasChanged",
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText == "StateHasChanged",
                _ => false
            });
    }

    private static Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, Location location, string componentName)
    {
        return Diagnostic.Create(descriptor, location, componentName);
    }

    private static bool InheritsFrom(INamedTypeSymbol symbol, string fullyQualifiedBaseType)
    {
        for (var current = symbol; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == fullyQualifiedBaseType)
            {
                return true;
            }
        }

        return false;
    }

    private static bool Implements(INamedTypeSymbol symbol, string fullyQualifiedInterfaceType)
    {
        return symbol.AllInterfaces.Any(@interface => @interface.ToDisplayString() == fullyQualifiedInterfaceType);
    }

    private static string RenderProxy(ComponentCandidate candidate)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using System.Diagnostics;");
        builder.AppendLine("using System.Threading.Tasks;");
        builder.AppendLine("using BlazorDevTools.Runtime;");
        builder.AppendLine("using Microsoft.AspNetCore.Components;");
        builder.AppendLine("using Microsoft.AspNetCore.Components.Rendering;");
        builder.AppendLine();

        if (candidate.Namespace is not null)
        {
            builder.Append("namespace ").Append(candidate.Namespace).AppendLine(";");
            builder.AppendLine();
        }

        builder.Append("public sealed class ").Append(candidate.ProxyTypeName).Append(" : ").Append(candidate.TypeName).AppendLine(", IDevToolsComponentProxy");
        builder.AppendLine("{");
        builder.AppendLine("    [Inject]");
        builder.AppendLine("    private ComponentTracker ComponentTracker { get; set; } = default!;");
        builder.AppendLine();
        builder.AppendLine("    [Inject]");
        builder.AppendLine("    private DevToolsAutoRefreshScheduler AutoRefreshScheduler { get; set; } = default!;");
        builder.AppendLine();
        builder.AppendLine("    [Inject]");
        builder.AppendLine("    private IDevToolsExternalComponentTracker? ExternalComponentTracker { get; set; }");
        builder.AppendLine();
        builder.AppendLine("    [CascadingParameter(Name = DevtoolsComponentBase.ParentComponentIdCascadeName)]");
        builder.AppendLine("    public string? ParentComponentId { get; set; }");
        builder.AppendLine();
        builder.Append("    public Type DevToolsOriginalComponentType => typeof(").Append(candidate.FullyQualifiedTypeName).AppendLine(");");
        builder.AppendLine();
        builder.AppendLine("    private string DomMarkerId => TrackingLifecycle.ComponentId;");
        builder.AppendLine();
        builder.AppendLine("    private DevToolsTrackedComponentLifecycle? trackingLifecycle;");
        builder.AppendLine();
        builder.AppendLine("    protected new void StateHasChanged()");
        builder.AppendLine("    {");
        builder.AppendLine("        TrackingLifecycle.MarkStateHasChanged();");
        builder.AppendLine("        base.StateHasChanged();");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    protected override void OnInitialized()");
        builder.AppendLine("    {");
        builder.AppendLine("        var startedAt = Stopwatch.GetTimestamp();");
        builder.AppendLine("        base.OnInitialized();");
        builder.AppendLine("        trackingLifecycle?.RecordOnInitialized(Stopwatch.GetElapsedTime(startedAt));");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    protected override async Task OnInitializedAsync()");
        builder.AppendLine("    {");
        builder.AppendLine("        var startedAt = Stopwatch.GetTimestamp();");
        builder.AppendLine("        await base.OnInitializedAsync();");
        builder.AppendLine("        trackingLifecycle?.RecordOnInitializedAsync(Stopwatch.GetElapsedTime(startedAt));");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public override async Task SetParametersAsync(ParameterView parameters)");
        builder.AppendLine("    {");
        builder.AppendLine("        var startedAt = Stopwatch.GetTimestamp();");
        builder.AppendLine("        var snapshots = DevToolsParameterSnapshotFactory.Create(parameters, nameof(ParentComponentId));");
        builder.AppendLine("        var injectedServices = ComponentInjectedServiceSnapshotFactory.Create(DevToolsOriginalComponentType);");
        builder.AppendLine("        var cascadingParameters = ComponentCascadingParameterSnapshotFactory.Create(DevToolsOriginalComponentType);");
        builder.AppendLine();
        builder.AppendLine("        await base.SetParametersAsync(parameters);");
        builder.AppendLine();
        builder.AppendLine("        TrackingLifecycle.ApplySnapshot(DevToolsOriginalComponentType, ParentComponentId, snapshots, injectedServices, cascadingParameters, DomMarkerId);");
        builder.AppendLine("        TrackingLifecycle.RecordOnParametersSet(Stopwatch.GetElapsedTime(startedAt));");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    protected override void BuildRenderTree(RenderTreeBuilder builder)");
        builder.AppendLine("    {");
        builder.AppendLine("        TrackingLifecycle.RenderWithParentScopeAndDomMarker(builder, base.BuildRenderTree);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    protected override void OnAfterRender(bool firstRender)");
        builder.AppendLine("    {");
        builder.AppendLine("        var startedAt = Stopwatch.GetTimestamp();");
        builder.AppendLine("        base.OnAfterRender(firstRender);");
        builder.AppendLine("        trackingLifecycle?.RecordOnAfterRender(Stopwatch.GetElapsedTime(startedAt));");
        builder.AppendLine("        trackingLifecycle?.OnAfterRender();");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public void Dispose()");
        builder.AppendLine("    {");
        builder.AppendLine("        trackingLifecycle?.Dispose();");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private DevToolsTrackedComponentLifecycle TrackingLifecycle => trackingLifecycle ??= new DevToolsTrackedComponentLifecycle(ComponentTracker, AutoRefreshScheduler, ExternalComponentTracker);");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static string RenderManifest(ImmutableArray<ComponentCandidate> candidates, string manifestSuffix)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using BlazorDevTools.Runtime;");
        builder.AppendLine();
        builder.AppendLine("namespace BlazorDevTools.Generated;");
        builder.AppendLine();
        builder.Append("public sealed class BlazorDevToolsGeneratedComponentProxyManifest_").Append(manifestSuffix).AppendLine(" : IDevToolsComponentProxyManifest");
        builder.AppendLine("{");
        builder.AppendLine("    public IReadOnlyList<DevToolsComponentProxyRegistration> GetRegistrations()");
        builder.AppendLine("    {");

        if (candidates.Length == 0)
        {
            builder.AppendLine("        return Array.Empty<DevToolsComponentProxyRegistration>();");
        }
        else
        {
            builder.AppendLine("        return new DevToolsComponentProxyRegistration[]");
            builder.AppendLine("        {");

            foreach (var candidate in candidates)
            {
                builder.Append("            new(typeof(").Append(candidate.FullyQualifiedTypeName).Append("), typeof(")
                    .Append(candidate.FullyQualifiedProxyTypeName)
                    .AppendLine(")),");
            }

            builder.AppendLine("        };");
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private sealed class ComponentCandidate
    {
        public ComponentCandidate(
            INamedTypeSymbol symbol,
            string? @namespace,
            string typeName,
            string proxyTypeName,
            string fullyQualifiedTypeName,
            string fullyQualifiedProxyTypeName)
        {
            Symbol = symbol;
            Namespace = @namespace;
            TypeName = typeName;
            ProxyTypeName = proxyTypeName;
            FullyQualifiedTypeName = fullyQualifiedTypeName;
            FullyQualifiedProxyTypeName = fullyQualifiedProxyTypeName;
        }

        public INamedTypeSymbol Symbol { get; }

        public string? Namespace { get; }

        public string TypeName { get; }

        public string ProxyTypeName { get; }

        public string FullyQualifiedTypeName { get; }

        public string FullyQualifiedProxyTypeName { get; }
    }

    private sealed class ComponentAnalysis
    {
        public ComponentAnalysis(INamedTypeSymbol symbol, ComponentCandidate? candidate, Diagnostic? diagnostic)
        {
            Symbol = symbol;
            Candidate = candidate;
            Diagnostic = diagnostic;
        }

        public INamedTypeSymbol Symbol { get; }

        public ComponentCandidate? Candidate { get; }

        public Diagnostic? Diagnostic { get; }
    }

    private readonly struct SkipResult
    {
        public SkipResult(DiagnosticDescriptor descriptor)
        {
            Descriptor = descriptor;
        }

        public DiagnosticDescriptor Descriptor { get; }
    }

    private static string SanitizeIdentifier(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        if (builder.Length == 0 || !char.IsLetter(builder[0]) && builder[0] != '_')
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }

    private sealed class ComponentCandidateSymbolEqualityComparer : IEqualityComparer<ComponentCandidate>
    {
        public static ComponentCandidateSymbolEqualityComparer Instance { get; } = new();

        public bool Equals(ComponentCandidate? x, ComponentCandidate? y)
        {
            return SymbolEqualityComparer.Default.Equals(x?.Symbol, y?.Symbol);
        }

        public int GetHashCode(ComponentCandidate obj)
        {
            return SymbolEqualityComparer.Default.GetHashCode(obj.Symbol);
        }
    }

    private sealed class ComponentAnalysisEqualityComparer : IEqualityComparer<ComponentAnalysis>
    {
        public static ComponentAnalysisEqualityComparer Instance { get; } = new();

        public bool Equals(ComponentAnalysis? x, ComponentAnalysis? y)
        {
            return SymbolEqualityComparer.Default.Equals(x?.Symbol, y?.Symbol)
                   && x?.Diagnostic?.Id == y?.Diagnostic?.Id;
        }

        public int GetHashCode(ComponentAnalysis obj)
        {
            unchecked
            {
                var hashCode = SymbolEqualityComparer.Default.GetHashCode(obj.Symbol);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(obj.Diagnostic?.Id ?? string.Empty);
                return hashCode;
            }
        }
    }
}

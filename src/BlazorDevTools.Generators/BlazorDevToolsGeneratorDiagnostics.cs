using Microsoft.CodeAnalysis;

namespace BlazorDevTools.Generators;

internal static class BlazorDevToolsGeneratorDiagnostics
{
    private const string Category = "BlazorDevTools.Generators";

    public static DiagnosticDescriptor AbstractComponentSkipped { get; } = Create(
        id: "BDTG001",
        title: "Abstract component skipped",
        messageFormat: "Blazor DevTools skipped component '{0}' because abstract components are not supported by the proxy generator.");

    public static DiagnosticDescriptor SealedComponentSkipped { get; } = Create(
        id: "BDTG002",
        title: "Sealed component skipped",
        messageFormat: "Blazor DevTools skipped component '{0}' because sealed components are not supported by the proxy generator.");

    public static DiagnosticDescriptor GenericComponentSkipped { get; } = Create(
        id: "BDTG003",
        title: "Generic component skipped",
        messageFormat: "Blazor DevTools skipped component '{0}' because generic components are not supported by the proxy generator MVP.");

    public static DiagnosticDescriptor NestedComponentSkipped { get; } = Create(
        id: "BDTG004",
        title: "Nested component skipped",
        messageFormat: "Blazor DevTools skipped component '{0}' because nested component classes are not supported by the proxy generator MVP.");

    public static DiagnosticDescriptor AlreadyTrackedComponentSkipped { get; } = Create(
        id: "BDTG005",
        title: "Already tracked component skipped",
        messageFormat: "Blazor DevTools skipped component '{0}' because it already inherits DevtoolsComponentBase.");

    public static DiagnosticDescriptor ExistingProxySkipped { get; } = Create(
        id: "BDTG006",
        title: "Existing proxy skipped",
        messageFormat: "Blazor DevTools skipped component '{0}' because it already implements IDevToolsComponentProxy.");

    public static DiagnosticDescriptor NonComponentBaseSkipped { get; } = Create(
        id: "BDTG007",
        title: "Non-ComponentBase type skipped",
        messageFormat: "Blazor DevTools skipped component '{0}' because it does not inherit ComponentBase.");

    public static DiagnosticDescriptor FrameworkComponentSkipped { get; } = Create(
        id: "BDTG008",
        title: "Framework component skipped",
        messageFormat: "Blazor DevTools skipped component '{0}' because framework/internal components under Microsoft.AspNetCore are not proxied.");

    public static DiagnosticDescriptor NonPartialComponentSkipped { get; } = Create(
        id: "BDTG009",
        title: "Non-partial component skipped",
        messageFormat: "Blazor DevTools skipped component '{0}' because non-partial component classes are not supported by the proxy generator MVP.");

    private static DiagnosticDescriptor Create(string id, string title, string messageFormat)
    {
        return new DiagnosticDescriptor(
            id,
            title,
            messageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);
    }
}

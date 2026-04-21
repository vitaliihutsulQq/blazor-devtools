namespace BlazorDevTools.Protocol;

public sealed record ComponentNode(
    string Id,
    string Name,
    string FullTypeName,
    string AssemblyName,
    string? DomMarkerId,
    IReadOnlyList<ComponentParameterSnapshot> Parameters,
    IReadOnlyList<ComponentInjectedServiceSnapshot> InjectedServices,
    int? RenderCount,
    IReadOnlyList<ComponentNode> Children);

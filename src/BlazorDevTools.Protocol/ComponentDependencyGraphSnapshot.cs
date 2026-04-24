namespace BlazorDevTools.Protocol;

public sealed record ComponentDependencyGraphSnapshot(
    IReadOnlyList<ComponentDependencyGraphNodeSnapshot> Nodes,
    IReadOnlyList<ComponentDependencyGraphEdgeSnapshot> Edges);

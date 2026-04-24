namespace BlazorDevTools.Protocol;

public sealed record ComponentDependencyGraphNodeSnapshot(
    string ComponentId,
    string Name,
    string FullTypeName);

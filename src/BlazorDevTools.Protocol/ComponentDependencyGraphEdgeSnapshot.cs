namespace BlazorDevTools.Protocol;

public sealed record ComponentDependencyGraphEdgeSnapshot(
    string SourceComponentId,
    string TargetComponentId,
    string EdgeType,
    string Summary,
    IReadOnlyList<string> RelatedValues,
    bool IsInferred,
    string? Details);

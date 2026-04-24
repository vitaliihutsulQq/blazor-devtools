namespace BlazorDevTools.Protocol;

public sealed record ComponentTreeSnapshot(
    DateTimeOffset CapturedAt,
    IReadOnlyList<ComponentNode> Roots,
    ComponentDependencyGraphSnapshot DependencyGraph);

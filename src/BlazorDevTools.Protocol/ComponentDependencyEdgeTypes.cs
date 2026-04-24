namespace BlazorDevTools.Protocol;

public static class ComponentDependencyEdgeTypes
{
    public const string ParentChild = nameof(ParentChild);
    public const string ParameterFlow = nameof(ParameterFlow);
    public const string CascadingDependency = nameof(CascadingDependency);
}

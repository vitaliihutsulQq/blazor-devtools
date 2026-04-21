namespace BlazorDevTools.Protocol;

public sealed record ComponentCascadingParameterSnapshot(
    string PropertyName,
    string ValueTypeName,
    string FullValueTypeName,
    string? ProviderHint);

namespace BlazorDevTools.Protocol;

public sealed record ComponentInjectedServiceSnapshot(string PropertyName, string ServiceTypeName, string FullServiceTypeName);

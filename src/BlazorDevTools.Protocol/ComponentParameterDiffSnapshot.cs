namespace BlazorDevTools.Protocol;

public sealed record ComponentParameterDiffSnapshot(
    string Name,
    string? PreviousValue,
    string? CurrentValue);

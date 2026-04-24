namespace BlazorDevTools.Protocol;

public sealed record ComponentRenderDiffSnapshot(
    int RenderSequence,
    DateTimeOffset RecordedAt,
    bool HasPreviousSnapshot,
    IReadOnlyList<ComponentParameterDiffSnapshot> ParameterChanges);

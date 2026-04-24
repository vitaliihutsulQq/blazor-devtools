namespace BlazorDevTools.Protocol;

public sealed record ComponentRenderDiffInfoSnapshot(
    ComponentRenderDiffSnapshot? LatestRenderDiff,
    IReadOnlyList<ComponentRenderDiffSnapshot> RecentRenderDiffs);

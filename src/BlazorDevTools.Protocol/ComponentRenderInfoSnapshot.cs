namespace BlazorDevTools.Protocol;

public sealed record ComponentRenderInfoSnapshot(
    ComponentRenderCauseSnapshot? LatestRenderCause,
    IReadOnlyList<ComponentRenderCauseSnapshot> RecentRenderCauses);

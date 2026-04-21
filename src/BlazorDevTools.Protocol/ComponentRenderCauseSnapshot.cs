namespace BlazorDevTools.Protocol;

public sealed record ComponentRenderCauseSnapshot(int RenderSequence, string Cause, bool IsApproximate, string? Details);

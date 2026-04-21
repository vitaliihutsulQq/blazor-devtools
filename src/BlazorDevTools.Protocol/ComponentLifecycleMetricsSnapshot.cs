namespace BlazorDevTools.Protocol;

public sealed record ComponentLifecycleMetricsSnapshot(
    double? TimeToFirstRenderMs,
    int RenderCount,
    double? AverageRenderTimeMs,
    int StateHasChangedCount,
    double? OnInitializedTimeMs,
    double? OnInitializedAsyncTimeMs,
    double? OnParametersSetTimeMs,
    double? OnAfterRenderTimeMs,
    double TotalRenderTimeMs);

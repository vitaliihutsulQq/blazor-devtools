using System.Diagnostics;
using BlazorDevTools.Protocol;

namespace BlazorDevTools.Runtime;

public sealed class ComponentLifecycleMetricsCollector
{
    private long? trackingStartedTimestamp;
    private int renderCount;
    private int stateHasChangedCount;
    private double totalRenderTimeMs;
    private double? timeToFirstRenderMs;
    private double? onInitializedTimeMs;
    private double? onInitializedAsyncTimeMs;
    private double? onParametersSetTimeMs;
    private double? onAfterRenderTimeMs;

    public void MarkTrackingStarted()
    {
        trackingStartedTimestamp ??= Stopwatch.GetTimestamp();
    }

    public void RecordOnInitialized(TimeSpan duration)
    {
        onInitializedTimeMs = duration.TotalMilliseconds;
    }

    public void RecordOnInitializedAsync(TimeSpan duration)
    {
        onInitializedAsyncTimeMs = duration.TotalMilliseconds;
    }

    public void RecordOnParametersSet(TimeSpan duration)
    {
        onParametersSetTimeMs = duration.TotalMilliseconds;
    }

    public void RecordOnAfterRender(TimeSpan duration)
    {
        onAfterRenderTimeMs = duration.TotalMilliseconds;
    }

    public void RecordStateHasChanged()
    {
        stateHasChangedCount++;
    }

    public void RecordRender(TimeSpan duration)
    {
        renderCount++;
        totalRenderTimeMs += duration.TotalMilliseconds;

        if (timeToFirstRenderMs is null && trackingStartedTimestamp is long started)
        {
            timeToFirstRenderMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        }
    }

    public ComponentLifecycleMetricsSnapshot BuildSnapshot()
    {
        return new ComponentLifecycleMetricsSnapshot(
            timeToFirstRenderMs,
            renderCount,
            renderCount == 0 ? null : totalRenderTimeMs / renderCount,
            stateHasChangedCount,
            onInitializedTimeMs,
            onInitializedAsyncTimeMs,
            onParametersSetTimeMs,
            onAfterRenderTimeMs,
            totalRenderTimeMs);
    }
}
